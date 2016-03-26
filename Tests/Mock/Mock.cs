using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace UnofficalSteamAuthenticator.Tests.Mock
{
    public class Mock
    {
        public delegate void MethodName([CallerMemberName] string method = "");

        public delegate T MethodName<out T>([CallerMemberName] string method = "");

        private readonly Dictionary<string, List<List<object>>> calls = new Dictionary<string, List<List<object>>>();
        private readonly Dictionary<string, Dictionary<List<object>, object>> responses = new Dictionary<string, Dictionary<List<object>, object>>();

        protected MethodName RegisterCall(params object[] args)
        {
            return method =>
            {
                if (!this.calls.ContainsKey(method))
                {
                    this.calls[method] = new List<List<object>>();
                }
                this.calls[method].Add(new List<object>(args));
            };
        }

        public int CallCount(string method)
        {
            return this.calls[method].Count;
        }

        public Action<object> WithArgs(string method, params object[] args)
        {
            return x =>
            {
                if (!this.responses.ContainsKey(method))
                {
                    this.responses[method] = new Dictionary<List<object>, object>();
                }
                this.responses[method][new List<object>(args)] = x;
            };
        }

        protected MethodName<object> GetResponse(params object[] args)
        {
            return method =>
            {
                return this.responses.ContainsKey(method)
                    ? this.responses[method].First(kv => CheckResponse(args, kv.Key)).Value
                    : null;
            };
        }

        private static bool CheckResponse(IReadOnlyList<object> args, IEnumerable<object> key)
        {
            var i = 0;
            return key.All(param => ++i >= args.Count || param.Equals(args[i - 1]));
        }
    }
}
