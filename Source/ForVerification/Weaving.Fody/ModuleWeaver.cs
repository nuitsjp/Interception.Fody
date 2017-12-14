using Mono.Cecil;
using Weaving;

public class ModuleWeaver
{
    public ModuleDefinition ModuleDefinition { get; set; }

    public void Execute()
    {
        var weaver = new Weaver();
        weaver.Weave(ModuleDefinition);
    }
}
