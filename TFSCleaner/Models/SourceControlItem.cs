using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.VersionControl.Client;
using SR.TFSCleaner.Helpers;

namespace SR.TFSCleaner.Models
{
    public class SourceControlItem : TreeViewItemViewModel
    {
        public SourceControlItem(Item _item)
            : base(null, true)
        {
            this.Item = _item;
            this.Name = _item.ServerItem.Substring(_item.ServerItem.LastIndexOf(@"/", System.StringComparison.Ordinal) + 1);
            this.Size = Item.ItemType == ItemType.File ? string.Format(new FileSizeFormatProvider(), " - Size: {0:fs}", Item.ContentLength) : "";
        }

        public SourceControlItem()
            : base(null, true)
        {
            this.Item = null;
            this.Name = "Dummy";
        }

        public Item Item { get; set; }

        public string Name { get; set; }

        public string ServerItem
        {
            get { return Item.ServerItem; }
        }

        protected async override void LoadChildren()
        {
            if (Item == null || Item.ItemType == ItemType.File) return;

            ItemSet itemSet = null;

            await Task.Run(() =>
            {
                ItemSpec spec = new ItemSpec(Item.ServerItem, RecursionType.OneLevel);
                itemSet = TfsShared.Instance.Vcs.GetItems(spec, VersionSpec.Latest, DeletedState.Any, ItemType.Any,
                    GetItemsOptions.IncludeBranchInfo);
            });
            
            foreach (Item item in itemSet.Items.OrderByDescending(f => f.ItemType == ItemType.Folder))
            {
                if (item.ItemId == this.Item.ItemId) continue;
                SourceControlItem srcItem = new SourceControlItem(item);

                if (item.ItemType == ItemType.File)
                    srcItem.IsExpanded = true;

                base.Children.Add(srcItem);
            }

            ScanFullSize();
        }

        public ItemType Type
        {
            get { return Item.ItemType; }
        }

        private string _size;
        public string Size
        {
            get { return _size; }
            set
            {
                if (value == _size) return;
                _size = value;
                this.OnPropertyChanged("Size");
            }
        }

        public async Task ScanFullSize()
        {
            this.Size = " - Calculating size...";
            await Task.Run(() =>
            {
                ItemSpec spec = new ItemSpec(Item.ServerItem, RecursionType.Full);
                ItemSet itemSet = TfsShared.Instance.Vcs.GetItems(spec, VersionSpec.Latest, DeletedState.Any,ItemType.Any, false);
                this.Size = string.Format(new FileSizeFormatProvider(), " - Size: {0:fs}", itemSet.Items.Sum(i => i.ContentLength));
                return;
            });
        }

        public string ToolTip
        {
            get { return string.Format("Checkin Date: {0}\nIs Branch: {1}\nChangeset Id:{2}", Item.CheckinDate, Item.IsBranch, Item.ChangesetId); }
        }

        public int ID
        {
            get { return Item.ItemId; }
        }
    }
}
