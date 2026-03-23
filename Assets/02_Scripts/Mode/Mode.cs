namespace Afterlife.Dev
{
    public abstract class Mode : Moonstone.Ore.Local.Entity
    {
        public void Enter<TParam>(TParam param = null) where TParam : ModeParam
        {
            enabled = true;
            OnEnter(param);
        }

        public void Exit<TParam>(TParam param = null) where TParam : ModeParam
        {
            OnExit(param);
            enabled = false;
        }

        protected virtual void OnEnter<TParam>(TParam param = null) where TParam : ModeParam { }
        protected virtual void OnExit<TParam>(TParam param = null) where TParam : ModeParam { }
    }
}