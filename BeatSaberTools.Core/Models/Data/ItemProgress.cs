namespace BeatSaberTools.Core.Models.Data
{
    public class ItemProgress<T>
    {
        public int TotalItems { get; set; }
        public int CompletedItems { get; set; }
        public T CurrentItem { get; set; }
        public double CurrentMapProgress { get; set; }
        public double TotalProgress
        {
            get
            {
                if (CompletedItems == TotalItems)
                {
                    return 1;
                }
                else
                {
                    return (CompletedItems + CurrentMapProgress) / TotalItems;
                }
            }
        }
    }
}
