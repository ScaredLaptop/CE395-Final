import azure.cognitiveservices.speech as speechsdk
import time
import json
import os

speech_key, service_region = "7f8a7cfad1e34221926d74577fcdbc36","centralus"

def translate_loop():
    endpoint_string = "wss://{}.stt.speech.microsoft.com/speech/universal/v2".format(service_region)
    translation_config = speechsdk.translation.SpeechTranslationConfig(
        subscription=speech_key,
        endpoint=endpoint_string,
        speech_recognition_language='en-US',
        target_languages=('de', 'sw', 'ar', 'en-US'))
    audio_config = speechsdk.audio.AudioOutputConfig(use_default_speaker=True)
    auto_detect_source_language_config = speechsdk.languageconfig.AutoDetectSourceLanguageConfig(languages=["en-US", "de-DE", "zh-CN"])
    recognizer = speechsdk.translation.TranslationRecognizer(
        translation_config=translation_config, 
        audio_config=audio_config,
        auto_detect_source_language_config=auto_detect_source_language_config)
    result = recognizer.recognize_once()
    speech_config = speechsdk.SpeechConfig(subscription=speech_key, region=service_region)
    speech_config.speech_synthesis_voice_name='en-US-AvaMultilingualNeural'
    speech_synthesizer = speechsdk.SpeechSynthesizer(speech_config=speech_config, audio_config=audio_config)

    if result.reason == speechsdk.ResultReason.TranslatedSpeech:
        print("""Recognized: {}
        English translation: {}""".format(
            result.text, result.translations['en-US']))
        speech_synthesis_result = speech_synthesizer.speak_text_async(result.translations['en-US']).get()
        if speech_synthesis_result.reason == speechsdk.ResultReason.SynthesizingAudioCompleted:
            print("Speech synthesized for text [{}]".format(result.translations['en-US']))
        elif speech_synthesis_result.reason == speechsdk.ResultReason.Canceled:
            cancellation_details = speech_synthesis_result.cancellation_details
            print("Speech synthesis canceled: {}".format(cancellation_details.reason))
            if cancellation_details.reason == speechsdk.CancellationReason.Error:
                if cancellation_details.error_details:
                    print("Error details: {}".format(cancellation_details.error_details))
                    print("Did you set the speech resource key and region values?")
    elif result.reason == speechsdk.ResultReason.RecognizedSpeech:
        print("Recognized: {}".format(result.text))
        detectedSrcLang = result.properties[speechsdk.PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult]
        print("Detected Language: {}".format(detectedSrcLang))
    elif result.reason == speechsdk.ResultReason.NoMatch:
        print("No speech could be recognized: {}".format(result.no_match_details))
    elif result.reason == speechsdk.ResultReason.Canceled:
        print("Translation canceled: {}".format(result.cancellation_details.reason))
        if result.cancellation_details.reason == speechsdk.CancellationReason.Error:
            print("Error details: {}".format(result.cancellation_details.error_details))

def speech_recognize_keyword_from_microphone():
    """performs keyword-triggered speech recognition with input microphone"""
    speech_config = speechsdk.SpeechConfig(subscription=speech_key, region=service_region)
    model = speechsdk.KeywordRecognitionModel("keyword.table")
    keyword = "Translate Please"
    speech_recognizer = speechsdk.SpeechRecognizer(speech_config=speech_config)
    done = False
    def stop_cb(evt: speechsdk.SessionEventArgs):
        """callback that signals to stop continuous recognition upon receiving an event `evt`"""
        print('CLOSING on {}'.format(evt))
        nonlocal done
        done = True
    def recognized_cb(evt: speechsdk.SpeechRecognitionEventArgs):
        """callback for recognized event"""
        if evt.result.reason == speechsdk.ResultReason.RecognizedKeyword:
            print('RECOGNIZED KEYWORD: {}'.format(evt))
            translate_loop()
    # Connect callbacks to the events fired by the speech recognizer
    speech_recognizer.recognized.connect(recognized_cb)
    # Start keyword recognition
    speech_recognizer.start_keyword_recognition(model)
    print('Say something starting with "{}" followed by whatever you want...'.format(keyword))
    while not done:
        time.sleep(.5)
    speech_recognizer.stop_keyword_recognition()

speech_recognize_keyword_from_microphone()