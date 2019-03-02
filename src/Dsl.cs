using System.Collections.Generic;

namespace Prgfx.Fusion
{
    public class DslFactory
    {
        protected Dictionary<string, IFusionDsl> registeredDsls;

        public DslFactory()
        {
            this.registeredDsls = new Dictionary<string, IFusionDsl>();
        }

        public void RegisterDsl(string identifier, IFusionDsl dsl)
        {
            this.registeredDsls.Add(identifier, dsl);
        }

        public IFusionDsl create(string identifier)
        {
            if (!registeredDsls.ContainsKey(identifier))
            {
                throw new DslException($"No DSL registered for identifier \"{identifier}\"");
            }
            return registeredDsls[identifier];
        }
    }

    public interface IFusionDsl
    {
        string Transpile(string code);
    }

    public class DslException : System.Exception
    {
        public DslException(string message) : base(message)
        {
        }
    }
}