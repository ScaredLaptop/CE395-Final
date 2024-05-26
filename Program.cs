using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;

class Program
{
    const string speechKey = "7f8a7cfad1e34221926d74577fcdbc36";
    const string serviceRegion = "centralus";

    static async Task TranslateLoopAsync()
    {
        var endpointString = $"wss://{serviceRegion}.stt.speech.microsoft.com/speech/universal/v2";
        var translationConfig = SpeechTranslationConfig.FromEndpoint(new Uri(endpointString), speechKey);
        translationConfig.SpeechRecognitionLanguage = "en-US";
        translationConfig.AddTargetLanguage("de");
        translationConfig.AddTargetLanguage("sw");
        translationConfig.AddTargetLanguage("ar");
        translationConfig.AddTargetLanguage("en-US");

        var audioConfig = AudioConfig.FromDefaultSpeakerOutput();
        var autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages(new string[] { "en-US", "de-DE", "zh-CN" });
        var recognizer = new TranslationRecognizer(translationConfig, autoDetectSourceLanguageConfig, audioConfig);
        var result = recognizer.RecognizeOnceAsync().GetAwaiter().GetResult();

        var speechConfig = SpeechConfig.FromSubscription(speechKey, serviceRegion);
        speechConfig.SpeechSynthesisVoiceName = "en-US-AvaMultilingualNeural";
        var speechSynthesizer = new SpeechSynthesizer(speechConfig, audioConfig);
        Console.WriteLine(result.Translations);
        if (result.Reason == ResultReason.TranslatedSpeech)
        {
            Console.WriteLine($"Recognized: {result.Text}");
            Console.WriteLine($"English translation: {result.Translations["en-US"]}");

            var synthesisResult = await speechSynthesizer.SpeakTextAsync(result.Translations["en-US"]);

            if (synthesisResult.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine($"Speech synthesized for text [{result.Translations["en-US"]}]");
            }
            else if (synthesisResult.Reason == ResultReason.Canceled)
            {
                var cancellationDetails = SpeechSynthesisCancellationDetails.FromResult(synthesisResult);
                Console.WriteLine($"Speech synthesis canceled: {cancellationDetails.Reason}");
                if (cancellationDetails.Reason == CancellationReason.Error && !string.IsNullOrEmpty(cancellationDetails.ErrorDetails))
                {
                    Console.WriteLine($"Error details: {cancellationDetails.ErrorDetails}");
                    Console.WriteLine("Did you set the speech resource key and region values?");
                }
            }
        }
        else if (result.Reason == ResultReason.RecognizedSpeech)
        {
            Console.WriteLine($"Recognized: {result.Text}");
            var detectedSrcLang = result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);
            Console.WriteLine($"Detected Language: {detectedSrcLang}");
        }
        else if (result.Reason == ResultReason.NoMatch)
        {
            Console.WriteLine("No speech could be recognized.");
        }
        else if (result.Reason == ResultReason.Canceled)
        {
            Console.WriteLine($"Error details: Cancelled");
        }
    }

    static async Task SpeechRecognizeKeywordFromMicrophoneAsync()
    {
        var speechConfig = SpeechConfig.FromSubscription(speechKey, serviceRegion);
        var model = KeywordRecognitionModel.FromFile("keyword.table");
        var keyword = "Translate Please";
        var speechRecognizer = new SpeechRecognizer(speechConfig);
        var done = new TaskCompletionSource<int>();

        speechRecognizer.Recognized += async (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedKeyword)
            {
                Console.WriteLine($"RECOGNIZED KEYWORD: {e.Result.Text}");
                await TranslateLoopAsync();
            }
        };

        speechRecognizer.SessionStopped += (s, e) =>
        {
            Console.WriteLine($"CLOSING on {e.SessionId}");
            done.TrySetResult(0);
        };

        await speechRecognizer.StartKeywordRecognitionAsync(model);
        Console.WriteLine($"Say something starting with \"{keyword}\" followed by whatever you want...");

        await done.Task;

        await speechRecognizer.StopKeywordRecognitionAsync();
    }

    static async Task Main(string[] args)
    {
        while (true)
        {
            await SpeechRecognizeKeywordFromMicrophoneAsync();
        }
    }
}
