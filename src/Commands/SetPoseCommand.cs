namespace Microdancer
{
    public sealed class SetPoseCommand : CommandBase
    {
        public SetPoseCommand(Service.Locator serviceLocator) : base(serviceLocator)
        {
        }

        [Command("setpose")]
        public void SetCPose(string[] _)
        {
            PrintError(
                $"/setpose has been removed. Please use /ppose from AutoVisor instead.");
        }
    }
}
