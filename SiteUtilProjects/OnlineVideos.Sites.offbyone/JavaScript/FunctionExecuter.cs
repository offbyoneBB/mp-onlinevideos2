using System;
using System.Collections.Generic;
using System.Linq;

namespace OnlineVideos.JavaScript
{
    public class FunctionExecuter
    {
        private IList<FunctionData> functions;

        public FunctionExecuter(IList<FunctionData> functions)
        {
            this.functions = functions;
        }

        public string Execute(string signature)
        {            
            foreach(var function in functions)
            {
                switch (function.Type)
                {
                    case FunctionTypes.Reverse :
                        {
                            signature = Reverse(signature);
                            break;
                        }
                    case FunctionTypes.Slice:
                        {                            
                            signature = Slice(signature, int.Parse(function.Parameters[1]));
                            break;
                        }
                    case FunctionTypes.Splice:
                        {
                            signature = Splice(signature, 0, int.Parse(function.Parameters[1]));
                            break;
                        }
                    case FunctionTypes.Swap:
                        {
                            signature = Swap(signature, int.Parse(function.Parameters[1]));
                            break;
                        }
                }
            }

            return signature;
        }

        string Swap(string signature, int b)
        {
            char[] tmpSignature = signature.ToArray();

            var c = tmpSignature[0];
            tmpSignature[0] = tmpSignature[b % tmpSignature.Length];
            tmpSignature[b] = c;

            return new string(tmpSignature);
        }

        string Reverse(string signature)
        {
            char[] tmp = signature.Reverse().ToArray();

            return new string(tmp);
        }

        string Splice(string signature, int start, int count)
        {
            return signature.Remove(start, count);
        }

        string Slice(string signature, int start)
        {
            string tmpSignature = String.Join("", signature);

            tmpSignature = tmpSignature.Substring(start);

            return tmpSignature;
        }
    }
}
