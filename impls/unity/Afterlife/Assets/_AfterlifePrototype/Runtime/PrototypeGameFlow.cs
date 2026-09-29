using System;
using Afterlife.Modules.Flow;

namespace Afterlife.Prototype
{
    public enum GameScreen { Title, Intro, Playing, Clear, GameOver }
    public enum GameFlowTrigger { Start, IntroCompleted, Won, Lost, ReturnToTitle }

    public interface IPrototypeGameSession
    {
        void Begin();
        void Stop();
    }

    public sealed class GameFlowResult
    {
        public bool Victory { get; }
        public int BestCleared { get; }
        public int Rebirths { get; }
        public int Kills { get; }

        public GameFlowResult(bool victory, int bestCleared, int rebirths, int kills)
        { Victory = victory; BestCleared = bestCleared; Rebirths = rebirths; Kills = kills; }
    }

    // 게임 규칙과 세션 수명은 어댑터에만 있다. Afterlife.Modules.Flow에는 게임 상태 이름도 없다.
    public sealed class PrototypeGameFlow : IDisposable
    {
        private readonly FlowMachine<GameScreen, GameFlowTrigger> _machine;
        public GameScreen Current => _machine.Current;
        public bool IsPlaying => _machine.IsStarted && !_machine.IsFaulted && Current == GameScreen.Playing;
        public GameFlowResult Result { get; private set; }
        public event Action<GameScreen> ScreenChanged;

        public PrototypeGameFlow(IPrototypeGameSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            _machine = new FlowMachine<GameScreen, GameFlowTrigger>(new[]
            {
                new FlowState<GameScreen>(GameScreen.Title, () => Result = null),
                new FlowState<GameScreen>(GameScreen.Intro),
                new FlowState<GameScreen>(GameScreen.Playing, session.Begin, session.Stop),
                new FlowState<GameScreen>(GameScreen.Clear),
                new FlowState<GameScreen>(GameScreen.GameOver)
            }, new[]
            {
                Route(GameScreen.Title, GameFlowTrigger.Start, GameScreen.Intro),
                Route(GameScreen.Intro, GameFlowTrigger.IntroCompleted, GameScreen.Playing),
                Route(GameScreen.Playing, GameFlowTrigger.Won, GameScreen.Clear),
                Route(GameScreen.Playing, GameFlowTrigger.Lost, GameScreen.GameOver),
                Route(GameScreen.Clear, GameFlowTrigger.ReturnToTitle, GameScreen.Title),
                Route(GameScreen.GameOver, GameFlowTrigger.ReturnToTitle, GameScreen.Title),
                Route(GameScreen.Playing, GameFlowTrigger.ReturnToTitle, GameScreen.Title),
                Route(GameScreen.GameOver, GameFlowTrigger.Start, GameScreen.Playing)
            });
            _machine.StateChanged += OnScreenChanged;
        }

        private static FlowTransition<GameScreen, GameFlowTrigger> Route(GameScreen from, GameFlowTrigger trigger, GameScreen to)
            => new FlowTransition<GameScreen, GameFlowTrigger>(from, trigger, to);
        private void OnScreenChanged(GameScreen screen) => ScreenChanged?.Invoke(screen);
        public void Initialize() => _machine.Start(GameScreen.Title);
        public bool StartGame() => _machine.TrySend(GameFlowTrigger.Start);
        public bool CompleteIntro() => _machine.TrySend(GameFlowTrigger.IntroCompleted);
        public bool ReturnToTitle() => _machine.TrySend(GameFlowTrigger.ReturnToTitle);
        public bool Retry()
        {
            if (Current != GameScreen.GameOver || _machine.IsTransitioning) return false;
            if (!_machine.TrySend(GameFlowTrigger.Start)) return false;
            Result = null;
            return true;
        }
        public bool Complete(GameFlowResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (!IsPlaying || _machine.IsTransitioning) return false;
            Result = result;
            return _machine.TrySend(result.Victory ? GameFlowTrigger.Won : GameFlowTrigger.Lost);
        }
        public void Dispose()
        {
            _machine.StateChanged -= OnScreenChanged;
            try { _machine.Dispose(); }
            finally { ScreenChanged = null; Result = null; }
        }
    }
}
