using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using Newtonsoft.Json;

class Program 
{

    class Interaction
    {
        public List<Message> Messages { get; set; } = new List<Message>();
    }

    public class Message
    {
        public string Speaker { get; set; }
        public string Text { get; set; }
    }

    
    async static Task FromStream(SpeechConfig speechConfig)
    {
        var stopwatch = Stopwatch.StartNew();

        // using var reader = new BinaryReader(File.OpenRead("audioFile.wav"));
        // using var audioInputStream = AudioInputStream.CreatePushStream();
        // using var audioConfig = AudioConfig.FromWavFileInput("audioFile.wav");
        var stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        var stringBuilder = new System.Text.StringBuilder();
        var interaction = new Interaction();
        // Create an audio stream from a wav file or from the default microphone
        using (var audioConfig = AudioConfig.FromWavFileInput("audioFile.wav"))
        {
            // Create a conversation transcriber using audio stream input
            using (var conversationTranscriber = new ConversationTranscriber(speechConfig, audioConfig))
            {
                // conversationTranscriber.Transcribing += (s, e) =>
                // {
                //     Console.WriteLine($"TRANSCRIBING: Text={e.Result.Text}");
                // };

                conversationTranscriber.Transcribed += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        interaction.Messages.Add(new Message()
                        {
                            Speaker = e.Result.SpeakerId,
                            Text = e.Result.Text
                        });
                        // stringBuilder.AppendLine($"{e.Result.Text}, Speaker ID={e.Result.SpeakerId}");
                        
                        Console.WriteLine($"TRANSCRIBED: Text={e.Result.Text} Speaker ID={e.Result.SpeakerId}");
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be transcribed.");
                    }
                };

                conversationTranscriber.Canceled += (s, e) =>
                {
                    Console.WriteLine($"CANCELED: Reason={e.Reason}");

                    if (e.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                        stopRecognition.TrySetResult(0);
                    }

                    stopRecognition.TrySetResult(0);
                };

                conversationTranscriber.SessionStopped += (s, e) =>
                {
                    Console.WriteLine("\n    Session stopped event.");
                    stopRecognition.TrySetResult(0);
                };

                await conversationTranscriber.StartTranscribingAsync();

                // Waits for completion. Use Task.WaitAny to keep the task rooted.
                Task.WaitAny(new[] { stopRecognition.Task });
                await conversationTranscriber.StopTranscribingAsync();
                
                
                // OpenAI service
                // Map text to json object
                
                
                
                // Console.WriteLine($"Final result: {stringBuilder.ToString()}");
                File.WriteAllText("transcription.txt", JsonConvert.SerializeObject(interaction, Formatting.Indented));
                
                
                Console.WriteLine($"Time elapsed: {stopwatch.Elapsed.Minutes} minutes {stopwatch.Elapsed.Seconds} seconds");
            }
        }
    }

    async static Task Main(string[] args)
    {
        var speechConfig = SpeechConfig.FromSubscription("a4cbe67cc2b04d74b89c727c3eddeb3d", "centralus");
        await FromStream(speechConfig);
    }
}