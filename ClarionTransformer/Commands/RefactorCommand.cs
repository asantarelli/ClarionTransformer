namespace ClarionTransformer.Commands
{
    /// <summary>Comando de menu fijo: transforma usando el perfil "Refactorizar".</summary>
    public class RefactorCommand : AbstractProfileTransformCommand
    {
        protected override string ProfileName => "Refactorizar";
    }
}
