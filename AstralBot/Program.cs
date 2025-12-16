using AstralBot.Bot;
using FChat;

namespace AstralBot
{
    record ArgPair(string Key, string? Value);

    public partial class Program
    {
        static ChatConnection? _Conn = null;

        static Astral? _AstralBot;

        public async static Task<int> Main(string[] args)
        {
            // parse launch args
            var argsDict = args.Select((val, i) => new { val, i }).Where(x => x.i % 2 == 0 && x.i + 1 < args.Length)
                .Select(x => new ArgPair(x.val, args[x.i + 1])).ToDictionary(x => x.Key.ToLower(), x => x.Value!, StringComparer.OrdinalIgnoreCase);

            string? account;
            string? password;
            string? character;
            string? botid;
            string? botversion;
            bool    verbose;

            try
            {
                account     = argsDict.TryGetValue("/account", out var a) ? a : throw new Exception();
                password    = argsDict.TryGetValue("/password", out var p) ? p : throw new Exception();
                character   = argsDict.TryGetValue("/character", out var c) ? c : throw new Exception();
                botid       = argsDict.TryGetValue("/botid", out var bi) ? bi : throw new Exception();
                botversion  = argsDict.TryGetValue("/botversion", out var bv) ? bv : throw new Exception();
                verbose     = args.Contains("/verbose", StringComparer.OrdinalIgnoreCase);

                if (account == null || password == null || character == null || botid == null || botversion == null) throw new Exception();
            }
            catch (Exception)
            {
                throw new Exception("Incorrect launch args.");
            }

            _Conn ??= new();
            _AstralBot ??= new(_Conn);

            using var cts = new CancellationTokenSource();
            ConsoleWriter.SetVerbosity(verbose);
            await RunChatAsync(_Conn, account, password, character, botid, botversion, false, cts.Token).ConfigureAwait(false);
            return 0;
        }

        static async Task<int> RunChatAsync(ChatConnection conn, string account, string pass, string character, string botname, string botversion, bool verbose, CancellationToken ct)
        {
            // Initialize Chat
            conn.CharacterListReceivedHandler   += CharacterListReceivedHandler;
            conn.AdReceivedHandler              += AdReceivedHandler;
            conn.BroadcastReceivedHandler       += BroadcastReceivedHandler;
            conn.ChannelDataUpdatedHandler      += ChannelDataUpdatedHandler;
            conn.ChannelMessageReceivedHandler  += ChannelMessageReceivedHandler;
            conn.ErrorReceivedHandler           += ErrorReceivedHandler;
            conn.FriendListReceivedHandler      += FriendListReceivedHandler;
            conn.PrivateMessageReceivedHandler  += PrivateMessageReceivedHandler;
            conn.ServerStatsReceivedHandler     += ServerStatsReceivedHandler;
            conn.SystemMessageReceivedHandler   += SystemMessageReceivedHandler;
            conn.TypingChangedHandler           += TypingChangedHandler;
            conn.UserJoinedChannelHandler       += UserJoinedChannelHandler;
            conn.UserKinksReceivedHandler       += UserKinksReceivedHandler;
            conn.UserLeftChannelHandler         += UserLeftChannelHandler;
            conn.UserLoggedHandler              += UserLoggedHandler;
            conn.UserProfileInfoReceivedHandler += UserProfileInfoReceivedHandler;
            conn.ChannelsReceivedHandler        += ChannelsReceivedHandler;

            const int maxRetries = 300;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                attempt++;

                // Recreate the TCSs for each attempt
                var quitTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                var disconnectedTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                var cancelTask = CancellationTokenExtensions.AsTask(ct);

                try
                {
                    // Try to connect
                    conn.Connect(account, pass, character, botname, botversion, quitTcs, verbose);

                    // Wait for completion
                    var completed = await Task.WhenAny(quitTcs.Task, cancelTask, disconnectedTcs.Task).ConfigureAwait(false);

                    if (completed == quitTcs.Task)
                    {
                        // Graceful quit
                        return await quitTcs.Task.ConfigureAwait(false);
                    }
                    else if (completed == cancelTask)
                    {
                        throw new OperationCanceledException(ct);
                    }
                    else if (completed == disconnectedTcs.Task)
                    {
                        Console.WriteLine($"Disconnected unexpectedly. Attempt {attempt} of {maxRetries}...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Connection error: {ex.Message}. Attempt {attempt} of {maxRetries}...");
                }

                // Optional: wait a bit before retrying
                await Task.Delay(TimeSpan.FromSeconds(60), CancellationToken.None);
            }

            // If we reach here, all attempts failed
            throw new Exception("Failed to connect after multiple attempts due to unexpected disconnections.");
        }
    }

    public static class CancellationTokenExtensions
    {
        public static Task AsTask(this CancellationToken ct)
        {
            if (!ct.CanBeCanceled)
                return Task.Delay(Timeout.Infinite, new CancellationToken(true));

            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            ct.Register(static s => ((TaskCompletionSource<object?>)s!).TrySetResult(null), tcs);
            return tcs.Task;
        }
    }
}