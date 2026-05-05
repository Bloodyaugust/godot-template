using System.Collections.Generic;

namespace godottemplate.Server;

public interface IAgentInspectable
{
    Dictionary<string, object> GetAgentProperties();
}
