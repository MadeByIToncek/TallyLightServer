using Fleck;
using Newtonsoft.Json.Linq;


namespace TallyLightServer
{
    internal class Program
    {
        static WebSocketSharp.WebSocket ws;
        static List<IWebSocketConnection> allSockets = new List<IWebSocketConnection>();
        static Dictionary<string, string> requestIdent = new Dictionary<string, string>();
        static void Main(string[] args)
        {
            var server = new WebSocketServer("ws://0.0.0.0:5555");
            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Console.WriteLine("Open!");
                    allSockets.Add(socket);
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine("Close!");
                    allSockets.Remove(socket);
                };
                socket.OnMessage = message =>
                {
                    Console.WriteLine(message);
                };
            });

            ws = new WebSocketSharp.WebSocket("ws://localhost:4444");
            ws.OnMessage += (sender, e) => handleIncoming(e.Data);
            ws.Connect();

            System.Timers.Timer tmr = new System.Timers.Timer();
            tmr.Elapsed += (sender, arg) =>
            {
                string ident = GenerateString(30);
                string target = "a_desktop";

                JObject request = new JObject(
                    new JProperty("op", 6),
                    new JProperty("d", new JObject(
                        new JProperty("requestId", ident),
                        new JProperty("requestType", "GetSourceActive"),
                        new JProperty("requestData", new JObject(
                            new JProperty("sourceName", target))))
                    ));
                requestIdent.Add(ident, target);
                ws.Send(request.ToString());
            };
            tmr.AutoReset = true;
            tmr.Interval = 1000;
            tmr.Start();

            Console.WriteLine("press any key to stop");
            Console.ReadKey();
        }

        private static void handleIncoming(string data)
        {
            //Console.WriteLine(data);
            JObject o = JObject.Parse(data);
            JObject d = o.GetValue("d").Value<JObject>();
            switch (o.GetValue("op").Value<int>())
            {
                case 0:
                    ws.Send("{\"op\": 1,\"d\": {\"rpcVersion\": 1}}");
                    break;
                case 2:
                    Console.WriteLine("[System] Negotiated RPC version " + o.GetValue("d").Value<JObject>().GetValue("negotiatedRpcVersion").Value<int>());
                    break;
                case 5:
                    switch (d.GetValue("eventType").Value<string>())
                    {
                        case "CurrentProgramSceneChanged":
                        case "StreamStateChanged":
                        case "RecordStateChanged":
                        case "InputActiveStateChanged":
                        case "InputShowStateChanged":
                        case "SceneItemCreated":
                        case "SceneItemRemoved":
                        case "SceneItemListReindexed":
                        case "SceneItemTransformChanged":
                        case "MediaInputPlaybackStarted":
                        case "MediaInputPlaybackEnded":
                        case "MediaInputActionTriggered":
                        case "SceneItemEnableStateChanged":
                            string ident = GenerateString(30);
                            string target = "a_desktop";

                            JObject request = new JObject(
                                new JProperty("op", 6),
                                new JProperty("d", new JObject(
                                    new JProperty("requestId", ident),
                                    new JProperty("requestType", "GetSourceActive"),
                                    new JProperty("requestData", new JObject(
                                        new JProperty("sourceName", target))))
                                ));
                            requestIdent.Add(ident, target);
                            ws.Send(request.ToString());
                            break;
                    }
                    break;
                case 7:
                    if (requestIdent.ContainsKey(d.GetValue("requestId").Value<string>()))
                    {
                        string val = requestIdent[d.GetValue("requestId").Value<string>()];
                        JObject output = new JObject();

                        output.Add(new JProperty("sourceIdent", val));
                        int active = d.GetValue("requestStatus").Value<JObject>().GetValue("result").Value<bool>() ?
                            (d.GetValue("responseData").Value<JObject>().GetValue("videoActive").Value<bool>() ?
                            1 : 0)
                            : 2;
                        output.Add(new JProperty("active", active));
                        foreach (var socket in allSockets.ToList())
                        {
                            socket.Send(output.ToString());
                        }
                    };
                    break;
            }
        }

        public static char GenerateChar(Random rng)
        {
            // 'Z' + 1 because the range is exclusive
            return (char)(rng.Next('A', 'Z' + 1));
        }

        public static string GenerateString(int length)
        {
            Random rng = new Random();
            char[] letters = new char[length];
            for (int i = 0; i < length; i++)
            {
                letters[i] = GenerateChar(rng);
            }
            return new string(letters);
        }
    }
}