using System;
using System.Threading.Tasks;

namespace Microdancer
{
    public sealed class InstrumentCommand : CommandBase
    {
        private readonly GameManager _gameManager;

        public InstrumentCommand(Service.Locator serviceLocator) : base(serviceLocator)
        {
            _gameManager = serviceLocator.Get<GameManager>();
        }

        [Command(
            "instrument",
            HelpMessage = "Equip an instrument (bard only)"
        )]
        public void Instrument(uint id)
        {
            _gameManager.OpenInstrument(id);
        }
    }
}
