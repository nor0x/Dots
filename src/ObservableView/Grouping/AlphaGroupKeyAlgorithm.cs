using ObservableView.Extensions;

namespace ObservableView.Grouping
{
    public class AlphaGroupKeyAlgorithm : GroupKeyAlgorithm<string>
    {
        private readonly bool upperCase;

        public AlphaGroupKeyAlgorithm(bool upperCase = true)
        {
            this.upperCase = upperCase;
        }

        public override string GetGroupKey(string value)
        {
            if (value == null)
            {
                return null;
            }

            if (value == "")
            {
                return "";
            }

            char firstChar = value[0];
            if (char.IsNumber(firstChar))
            {
                return "#";
            }

            if (this.upperCase)
            {
                return firstChar.ToString().ToUpper();
            }

            return firstChar.ToString().ToLower();
        }
    }
}
