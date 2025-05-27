using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace DevApps.AI
{
    internal enum IAType
    {
        GPT,
        MISTRAL
    }


    internal static class Service
    {
        internal static IAType type = IAType.MISTRAL;

        private static Task<string>? iaTask = null;
        private static CancellationTokenSource? source = null;
        private static List<string> messages = new List<string>();

        internal static event EventHandler? MessageReceived;

        internal class MessageReceivedEventArgs : EventArgs
        {
            public string Response { get; set; }
        }

        private static void OnMessageReceived()
        {
            MessageReceived?.Invoke(null, new EventArgs());//MessageReceivedEventArgs
        }


        static Service()
        {
            
        }

        internal static bool IsRunning => iaTask != null && iaTask.IsCompleted == false;

        internal static void Cancel()
        {
            if (IsRunning)
            {
                source?.Cancel();
                iaTask.Wait(5000);
                iaTask = null;
                source = null;
            }
        }

        internal static void Send(string text)
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Une requête est déjà en cours.");
            }

            messages.Add(text);

            source = new CancellationTokenSource();

            var mainId = Thread.CurrentThread.ManagedThreadId;

            iaTask = Task.Run(SendMessageFunc, source.Token);
            iaTask.GetAwaiter().OnCompleted(ReceiveMessageFunc);
            /*iaTask.GetAwaiter().OnCompleted(() =>
            {
                if (iaTask.IsCompleted == true)
                {
                    var id = Thread.CurrentThread.ManagedThreadId;

                    var response = iaTask.Result;
                    if (String.IsNullOrEmpty(response) == false)
                    {
                        try
                        {
                            Program.ParseCommands(response);
                        }
                        catch (Exception ex)
                        {
                            messages.Add(ex.Message);
                        }
                        OnMessageReceived();
                    }
                }
                else
                {
                    messages.Add("Impossible de transmettre le contexte du projet");
                }

                source = null;
                iaTask = null;
            });*/
        }

        private static async Task<string?> SendMessageFunc()
        {
            /*if (type == IAType.GPT)
            {
                try
                {
                    iaTask = ChatGPT.Send(PopupInput.Text);
                    iaTask.GetAwaiter().OnCompleted(() =>
                    {
                        var response = iaTask.Result;
                        if (ChatGPT.TryParseError(response, out ChatGPT.ErrorResponse? errorResponse))
                            MessageBox.Show(errorResponse?.error?.message);
                        else
                            Program.ParseCommands(response);
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    MessageBox.Show(ex.Message);
                }
            }
            else*/
            var id = Thread.CurrentThread.ManagedThreadId;

            if (type == IAType.MISTRAL)
            {
                try
                {
                    var fileTask = Mistral.SendFile(Profile.GetProjectBytes(), "devapps.json");
                    await fileTask.WaitAsync(source.Token);

                    if (source.IsCancellationRequested == true)
                    {
                        return String.Empty;
                    }

                    if (fileTask.IsCompleted == false)
                    {
                        return "Impossible de transmettre le contexte du projet";
                    }

                    var msgTask = Mistral.Send(messages.Last());
                    await fileTask.WaitAsync(source.Token);

                    if (source.IsCancellationRequested == true)
                    {
                        return String.Empty;
                    }

                    var response = msgTask.Result;

                    if (Mistral.TryParseError(response, out Mistral.ErrorResponse? errorResponse))
                        return errorResponse?.message;
                    else if (Mistral.TryParseResponse(response, out string? message))
                        return message;
                    else
                        return "Format de réponse non prise en charge";
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }

            return String.Empty;
        }

        private static void ReceiveMessageFunc()
        {
            if (iaTask?.IsCompleted == true)
            {
                var id = Thread.CurrentThread.ManagedThreadId;

                var response = iaTask.Result;
                if (String.IsNullOrEmpty(response) == false)
                {
                    try
                    {
                        Program.ParseCommands(response);
                    }
                    catch (Exception ex)
                    {
                        messages.Add(ex.Message);
                    }
                    OnMessageReceived();
                }
            }
            else
            {
                messages.Add("Impossible de transmettre le contexte du projet");
            }

            source = null;
            iaTask = null;
        }

    }
}
