using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;

using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;


public class Program
{
    static readonly string speechSubscriptionKey = "c32eb7ad10524e13a841465f9924dbf7";
    static readonly string speechServiceRegion = "eastus";

    static async Task Main()
    {
        await MultiLingualTranslation();
        Console.WriteLine("Please press <Return> to continue.");
        Console.ReadLine();
    }

    public static async Task RecognizeSpeechAsync()
    {
        // Creates an instance of a speech config with specified subscription key and service region.
        // Replace with your own subscription key // and service region (e.g., "westus").
        var config = SpeechConfig.FromSubscription(speechSubscriptionKey, speechServiceRegion);

        using (var recognizer = new SpeechRecognizer(config))
        {
            Console.WriteLine("Say something...");

            // Starts speech recognition, and returns after a single utterance is recognized. The end of a
            // single utterance is determined by listening for silence at the end or until a maximum of 15
            // seconds of audio is processed.  The task returns the recognition text as result. 
            // Note: Since RecognizeOnceAsync() returns only a single utterance, it is suitable only for single
            // shot recognition like command or query. 
            // For long-running multi-utterance recognition, use StartContinuousRecognitionAsync() instead.
            var result = await recognizer.RecognizeOnceAsync();

            // Checks result.
            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                Console.WriteLine($"We recognized: {result.Text}");
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = CancellationDetails.FromResult(result);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                    Console.WriteLine($"CANCELED: Did you update the subscription info?");
                }
            }
        }
    }


    //Ejemplo 6 (Aprobado)
    static async Task TranslateSpeechAsync6()
    {
        var translationConfig =
            SpeechTranslationConfig.FromSubscription(speechSubscriptionKey, speechServiceRegion);

        var fromLanguage = "en-US";
        var toLanguages = new List<string> { "it", "fr", "de" };
        translationConfig.SpeechRecognitionLanguage = fromLanguage;
        toLanguages.ForEach(translationConfig.AddTargetLanguage);

        using var audioConfig = AudioConfig.FromWavFileInput("YourAudioFile.wav");
        using var recognizer = new TranslationRecognizer(translationConfig, audioConfig);
    }

    //Ejemplo 7 (Aprobado)
    static async Task TranslateSpeechAsync7()
    {
        var translationConfig =
            SpeechTranslationConfig.FromSubscription(speechSubscriptionKey, speechServiceRegion);

        var fromLanguage = "en-US";
        var toLanguages = new List<string> { "it", "fr", "de", "es" };
        translationConfig.SpeechRecognitionLanguage = fromLanguage;
        toLanguages.ForEach(translationConfig.AddTargetLanguage);

        using var recognizer = new TranslationRecognizer(translationConfig);

        Console.Write($"Say something in '{fromLanguage}' and ");
        Console.WriteLine($"we'll translate into '{string.Join("', '", toLanguages)}'.\n");

        var result = await recognizer.RecognizeOnceAsync();
        if (result.Reason == ResultReason.TranslatedSpeech)
        {
            Console.WriteLine($"Recognized: \"{result.Text}\":");
            foreach (var (language, translation) in result.Translations)
            {
                Console.WriteLine($"Translated into '{language}': {translation}");
            }
        }
    }

    //Ejemplo 8 (Aprobado)
    static async Task TranslateSpeechAsync8()
    {
        var translationConfig =
            SpeechTranslationConfig.FromSubscription(speechSubscriptionKey, speechServiceRegion);

        var fromLanguage = "en-US";
        var toLanguage = "de";
        translationConfig.SpeechRecognitionLanguage = fromLanguage;
        translationConfig.AddTargetLanguage(toLanguage);

        // See: https://aka.ms/speech/sdkregion#standard-and-neural-voices
        translationConfig.VoiceName = "de-DE-Hedda";

        using var recognizer = new TranslationRecognizer(translationConfig);

        recognizer.Synthesizing += (_, e) =>
        {
            var audio = e.Result.GetAudio();
            Console.WriteLine($"Audio synthesized: {audio.Length:#,0} byte(s) {(audio.Length == 0 ? "(Complete)" : "")}");

            if (audio.Length > 0)
            {
                File.WriteAllBytes("YourAudioFile.wav", audio);
            }
        };

        Console.Write($"Say something in '{fromLanguage}' and ");
        Console.WriteLine($"we'll translate into '{toLanguage}'.\n");

        var result = await recognizer.RecognizeOnceAsync();
        if (result.Reason == ResultReason.TranslatedSpeech)
        {
            Console.WriteLine($"Recognized: \"{result.Text}\"");
            Console.WriteLine($"Translated into '{toLanguage}': {result.Translations[toLanguage]}");
        }
    }

    //Ejemplo 9 (Aprobado)
    static async Task TranslateSpeechAsync9()
    {

        var translationConfig =
            SpeechTranslationConfig.FromSubscription(speechSubscriptionKey, speechServiceRegion);

        var fromLanguage = "en-US";
        var toLanguages = new List<string> { "de", "en", "it", "pt", "zh-Hans" };
        translationConfig.SpeechRecognitionLanguage = fromLanguage;
        toLanguages.ForEach(translationConfig.AddTargetLanguage);

        using var recognizer = new TranslationRecognizer(translationConfig);

        Console.Write($"Say something in '{fromLanguage}' and ");
        Console.WriteLine($"we'll translate into '{string.Join("', '", toLanguages)}'.\n");

        var result = await recognizer.RecognizeOnceAsync();
        if (result.Reason == ResultReason.TranslatedSpeech)
        {
            // See: https://aka.ms/speech/sdkregion#standard-and-neural-voices
            var languageToVoiceMap = new Dictionary<string, string>
            {
                ["de"] = "de-DE-KatjaNeural",
                ["en"] = "en-US-AriaNeural",
                ["it"] = "it-IT-ElsaNeural",
                ["pt"] = "pt-BR-FranciscaNeural",

                ["zh-Hans"] = "zh-CN-XiaoxiaoNeural"
            };

            Console.WriteLine($"Recognized: \"{result.Text}\"");

            foreach (var (language, translation) in result.Translations)
            {
                Console.WriteLine($"Translated into '{language}': {translation}");

                var speechConfig =
                    SpeechConfig.FromSubscription(
                        speechSubscriptionKey, speechServiceRegion);
                speechConfig.SpeechSynthesisVoiceName = languageToVoiceMap[language];

                using var audioConfig = AudioConfig.FromWavFileOutput($"{language}-translation.wav");
                using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);

                await synthesizer.SpeakTextAsync(translation);
            }
        }
    }

    //Ejemplo 10 (En revisión)
    static async Task MultiLingualTranslation()
    {
        //var audioFile = req.Form.Files[0].OpenReadStream();

        var region = speechServiceRegion;

        var endpointString = $"wss://{region}.stt.speech.microsoft.com/speech/universal/v2";
        var endpointUrl = new Uri(endpointString);

        var config = SpeechTranslationConfig.FromEndpoint(endpointUrl, speechSubscriptionKey);

        string fromLanguage = "en-US";
        var toLanguages = new List<string> { "es", "en", "fr", "de", "pt", "it"};

        config.SpeechRecognitionLanguage = fromLanguage;
        toLanguages.ForEach(config.AddTargetLanguage);

        config.SetProperty(PropertyId.SpeechServiceConnection_ContinuousLanguageIdPriority, "Latency");
        var autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages(new string[] { "es-MX", "en-US", "fr-FR", "de-DE", "pt-BR", "it-IT" });

        var stopTranslation = new TaskCompletionSource<int>();

        using (var audioMic = AudioConfig.FromDefaultMicrophoneInput())
        {
            using (var recognizer = new TranslationRecognizer(config, autoDetectSourceLanguageConfig, audioMic))
            {
                recognizer.Recognizing += (s, e) =>
                {
                    var lidResult = e.Result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);

                    Console.WriteLine($"Recognizing in '{lidResult}': Text={e.Result.Text}\n");
                    foreach (var element in e.Result.Translations)
                    {
                        Console.WriteLine($"Translating into '{element.Key}': {element.Value}\n");
                    }
                };

                recognizer.Recognized += (s, e) => {
                    if (e.Result.Reason == ResultReason.TranslatedSpeech)
                    {
                        var lidResult = e.Result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);

                        Console.WriteLine($"Recognized in '{lidResult}': Text={e.Result.Text}\n");
                        foreach (var element in e.Result.Translations)
                        {
                            Console.WriteLine($"Translated into '{element.Key}': {element.Value}");
                        }
                    }
                    else if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        Console.WriteLine($"Recognized: Text={e.Result.Text}\n");
                        Console.WriteLine($"Speech not translated.\n");
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                };

                recognizer.Canceled += (s, e) =>
                {
                    Console.WriteLine($"CANCELED: Reason={e.Reason}");

                    if (e.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                    }

                    stopTranslation.TrySetResult(0);
                };

                recognizer.SpeechStartDetected += (s, e) => {
                    Console.WriteLine("\nSpeech start detected event.");
                };

                recognizer.SpeechEndDetected += (s, e) => {
                    Console.WriteLine("\nSpeech end detected event.");
                };

                recognizer.SessionStarted += (s, e) => {
                    Console.WriteLine("\nSession started event.");
                };

                recognizer.SessionStopped += (s, e) => {
                    Console.WriteLine("\nSession stopped event.");
                    Console.WriteLine($"\nStop translation.");
                    stopTranslation.TrySetResult(0);
                };

                // Start continuous recognition. Use StopContinuousRecognitionAsync() to stop recognition.
                Console.WriteLine("Start translation...");
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                Task.WaitAny(new[] { stopTranslation.Task });
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
        }
    }


 

    public static async Task RecognizeOnceSpeechTranslationAsync()
    {
        var region = speechServiceRegion;
        // Currently the v2 endpoint is required. In a future SDK release you won't need to set it.
        var endpointString = $"wss://{region}.stt.speech.microsoft.com/speech/universal/v2";
        var endpointUrl = new Uri(endpointString);

        var config = SpeechTranslationConfig.FromEndpoint(endpointUrl, speechSubscriptionKey);

        config.SetProperty(PropertyId.SpeechServiceConnection_SingleLanguageIdPriority, "Latency");

        // Source language is required, but currently ignored. 
        string fromLanguage = "en-US";
        config.SpeechRecognitionLanguage = fromLanguage;

        var toLanguages = new List<string> { "es", "en", "fr", "de", "pt", "it" };
        toLanguages.ForEach(config.AddTargetLanguage);

        var autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages(new string[] { "en-US", "de-DE", "zh-CN" });

        using var audioMic = AudioConfig.FromDefaultMicrophoneInput();
        //using var audioFile = req.Form.Files[0].OpenReadStream();

        using (var recognizer = new TranslationRecognizer(
            config,
            autoDetectSourceLanguageConfig,
            audioMic))
        {

            Console.WriteLine("Say something or read from file...");
            var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

            if (result.Reason == ResultReason.TranslatedSpeech)
            {
                var lidResult = result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);

                Console.WriteLine($"RECOGNIZED in '{lidResult}': Text={result.Text}\n");
                foreach (var element in result.Translations)
                {
                    Console.WriteLine($"TRANSLATED into '{element.Key}': {element.Value}\n");
                }
            }
        }
    }

}