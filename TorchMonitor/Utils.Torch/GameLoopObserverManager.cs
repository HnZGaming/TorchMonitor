using Torch.API;
using Torch.Managers;
using Torch.Managers.PatchManager;

namespace Utils.Torch
{
    internal sealed class GameLoopObserverManager : Manager
    {
#pragma warning disable 649
        [DependencyAttribute(Ordered = false)]
        readonly PatchManager _patchMgr;
#pragma warning restore 649

        PatchContext _patchContext;

        GameLoopObserverManager(ITorchBase torchInstance) : base(torchInstance)
        {
        }

        public static void Add(ITorchBase torch)
        {
            var mngr = new GameLoopObserverManager(torch);
            torch.Managers.AddManager(mngr);
        }

        public override void Attach()
        {
            base.Attach();

            _patchContext = _patchMgr.AcquireContext();
            GameLoopObserver.Patch(_patchContext);
        }

        public override void Detach()
        {
            base.Detach();

            _patchMgr.FreeContext(_patchContext);
        }
    }
}