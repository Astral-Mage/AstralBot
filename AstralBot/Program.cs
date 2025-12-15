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
            var quitTcs         = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var disconnectedTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cancelTask      = CancellationTokenExtensions.AsTask(ct);

            try
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


                // Connect to Chat
                conn.Connect(account, pass, character, botname, botversion, quitTcs, verbose);

                // Wait for Chat activity or Cancellation
                var completed = await Task.WhenAny(quitTcs.Task, cancelTask, disconnectedTcs.Task).ConfigureAwait(false);
                if (completed == quitTcs.Task) return await quitTcs.Task.ConfigureAwait(false);
                if (completed == cancelTask) return 0;
                if (completed == disconnectedTcs.Task)
                {
                    Console.WriteLine("Disconnected from chat.");
                    return 2;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in RunChatAsync: {ex}");
                return 105;
            }
            finally
            {
                // Clean up resources
                try { conn?.Disconnect(); } catch { }
            }

            return 0;
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