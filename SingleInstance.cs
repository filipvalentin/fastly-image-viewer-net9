using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

public interface ISingleInstanceApp {
	bool SignalExternalCommandLineArgs(IList<string> args);
}

public static class SingleInstance<TApplication> where TApplication : Application, ISingleInstanceApp {
	private static Mutex mutex;
	private static string appId;
	private static CancellationTokenSource cts;
	private const string PipeNameSuffix = "_SingleInstancePipe";

	public static bool InitializeAsFirstInstance(string uniqueName) {
		appId = uniqueName + "_" + Environment.UserName;
		bool isFirstInstance;
		mutex = new Mutex(true, appId, out isFirstInstance);

		if (isFirstInstance) {
			StartPipeServerAsync();
		}
		else {
			SendArgsToFirstInstance(Environment.GetCommandLineArgs());
		}

		return isFirstInstance;
	}

	public static void Cleanup() {
		cts?.Cancel();
		mutex?.Dispose();
	}

	private static void SendArgsToFirstInstance(string[] args) {
		try {
			using NamedPipeClientStream client = new NamedPipeClientStream(".", appId + PipeNameSuffix, PipeDirection.Out);
			client.Connect(2000); // Wait max 2 seconds

			string joinedArgs = string.Join("\n", args);
			byte[] data = Encoding.UTF8.GetBytes(joinedArgs);
			client.Write(data, 0, data.Length);
		}
		catch {
			// Pipe might not be ready or running
		}
	}

	private static async void StartPipeServerAsync() {
		cts = new CancellationTokenSource();
		var token = cts.Token;

		_ = Task.Run(async () => {
			while (!token.IsCancellationRequested) {
				using NamedPipeServerStream server = new NamedPipeServerStream(appId + PipeNameSuffix, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
				await server.WaitForConnectionAsync(token);

				using MemoryStream ms = new();
				await server.CopyToAsync(ms, token);
				string argsRaw = Encoding.UTF8.GetString(ms.ToArray());
				var argsList = new List<string>(argsRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries));

				Application.Current.Dispatcher.Invoke(() => {
					((TApplication)Application.Current).SignalExternalCommandLineArgs(argsList);
				});
			}
		}, token);
	}
}
