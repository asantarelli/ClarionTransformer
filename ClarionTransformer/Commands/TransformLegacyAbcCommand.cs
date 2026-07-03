namespace ClarionTransformer.Commands
{
    /// <summary>Comando de menu fijo: transforma usando el perfil "Legacy -> ABC".</summary>
    public class TransformLegacyAbcCommand : AbstractProfileTransformCommand
    {
        protected override string ProfileName => "Legacy -> ABC";
    }
}
