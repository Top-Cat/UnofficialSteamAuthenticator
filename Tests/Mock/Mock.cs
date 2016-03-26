using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace UnofficalSteamAuthenticator.Tests.Mock
{
    public class Mock
    {
        public delegate void MethodName([CallerMemberName] string method = "");

        public delegate T MethodName<out T>([CallerMemberName] string method = "");

        private readonly List<Tuple<string, List<object>>> calls;
        private readonly Dictionary<string, Dictionary<List<object>, object>> responses;

        protected Mock(List<Tuple<string, List<object>>> calls = null, Dictionary<string, Dictionary<List<object>, object>> responses = null)
        {
            this.calls = calls ?? new List<Tuple<string, List<object>>>();
            this.responses = responses ?? new Dictionary<string, Dictionary<List<object>, object>>();
        }

        protected MethodName RegisterCall(params object[] args)
        {
            return method =>
            {
                this.calls.Add(new Tuple<string, List<object>>(method, new List<object>(args)));
            };
        }

        public int CallCount(string method, params object[] args)
        {
            return this.calls.Sum(callMethod => callMethod.Item1 == method && CheckResponse(callMethod.Item2, args) ? 1 : 0);
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
                    ? this.responses[method].FirstOrDefault(kv => CheckResponse(args, kv.Key)).Value
                    : null;
            };
        }

        private static bool CheckResponse(IReadOnlyList<object> args, IEnumerable<object> key)
        {
            var i = 0;
            // If we've checked all the args,
            // the arg is a function that returns true (matcher)
            // or the arg equals the param
            return key.All(param => ++i > args.Count ||
                (param is MulticastDelegate && ((MulticastDelegate) param).GetMethodInfo().ReturnType == typeof(bool) && (bool) ((MulticastDelegate) param).DynamicInvoke(args[i - 1])) ||
                param.Equals(args[i - 1]));
        }


        public Mock Clone()
        {
            return (Mock) Activator.CreateInstance(
                this.GetType(),
                this.calls.Select(call => new Tuple<string, List<object>>(call.Item1, call.Item2)).ToList(),
                this.responses.Select(kv => new KeyValuePair<string, Dictionary<List<object>, object>>(kv.Key, kv.Value)).ToDictionary(pair => pair.Key, pair => pair.Value)
            );
        }
    }
}
