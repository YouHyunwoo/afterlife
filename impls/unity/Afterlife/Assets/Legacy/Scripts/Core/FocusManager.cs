namespace Afterlife.Core
{
    public class FocusManager : ManagerBase
    {
        public UI.View Target;

        public bool IsFocused => Target != null && Target.IsOpen;

        public override void SetUp()
        {
            Target = null;
        }

        public override void TearDown()
        {
            Target = null;
        }

        public void Focus(UI.View view)
        {
            Clear();
            Target = view;
            Target.Show();
        }

        public void Clear()
        {
            if (Target != null) { Target.Hide(); }
            Target = null;
        }
    }
}