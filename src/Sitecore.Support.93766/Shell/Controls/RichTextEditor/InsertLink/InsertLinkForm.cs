using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
using Sitecore.Links;
using Sitecore.Resources.Media;
using Sitecore.Shell.Framework;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using System;

namespace Sitecore.Support.Shell.Controls.RichTextEditor.InsertLink
{
    public class InsertLinkForm : DialogForm
    {
        protected DataContext InternalLinkDataContext;
        protected Tab InternalLinkTab;
        protected TreeviewEx InternalLinkTreeview;
        protected DataContext MediaDataContext;
        protected Tab MediaTab;
        protected TreeviewEx MediaTreeview;
        protected Tabstrip Tabs;

        private void AdjustTabContentWindow()
        {
            if ((this.Tabs.Active == 0) && UIUtil.IsFirefox())
            {
                string str = "adjusted";
                string str2 = "false";
                object obj2 = base.ServerProperties[str];
                if (obj2 != null)
                {
                    str2 = obj2.ToString();
                }
                if (str2 == "false")
                {
                    base.ServerProperties[str] = "true";
                    SheerResponse.Eval("scForm.browser.adjustFillParentElements()");
                }
            }
        }

        private Item GetCurrentItem(Message message)
        {
            Item selectionItem;
            Assert.ArgumentNotNull(message, "message");
            string str = message["id"];
            if (this.Tabs.Active == 0)
            {
                selectionItem = this.InternalLinkTreeview.GetSelectionItem();
            }
            else
            {
                selectionItem = this.MediaTreeview.GetSelectionItem();
            }
            if (selectionItem == null)
            {
                return null;
            }
            if (!string.IsNullOrEmpty(str) && ID.IsID(str))
            {
                return selectionItem.Database.GetItem(ID.Parse(str), selectionItem.Language,
                    Sitecore.Data.Version.Latest);
            }
            return selectionItem;
        }

        private string GetMediaUrl(Item item)
        {
            Assert.ArgumentNotNull(item, "item");
            return MediaManager.GetMediaUrl(item, MediaUrlOptions.GetShellOptions());
        }

        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            if (message.Name == "item:load")
            {
                this.LoadItem(message);
            }
            else
            {
                Dispatcher.Dispatch(message, this.GetCurrentItem(message));
                base.HandleMessage(message);
            }
        }

        private void LoadItem(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            Item selectionItem = this.MediaTreeview.GetSelectionItem();
            Language language = Context.Language;
            if (selectionItem != null)
            {
                language = selectionItem.Language;
            }
            Item item = Client.ContentDatabase.GetItem(ID.Parse(message["id"]), language);
            if (item != null)
            {
                this.MediaDataContext.SetFolder(item.Uri);
                this.MediaTreeview.SetSelectedItem(item);
            }
        }

        private void OnActiveTabChanged(object sender, EventArgs args)
        {
            this.SetUploadButtonAvailability();
            this.AdjustTabContentWindow();
        }

        protected override void OnCancel(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            if (this.Mode == "webedit")
            {
                base.OnCancel(sender, args);
            }
            else
            {
                SheerResponse.Eval("scCancel()");
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            this.Tabs.OnChange += new EventHandler(this.OnActiveTabChanged);
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                this.Mode = WebUtil.GetQueryString("mo");
                this.InternalLinkDataContext.GetFromQueryString();
                this.MediaDataContext.GetFromQueryString();
                string queryString = WebUtil.GetQueryString("fo");
                if (queryString.Length > 0)
                {
                    if (!string.IsNullOrEmpty(WebUtil.GetQueryString("databasename")))
                    {
                        this.InternalLinkDataContext.Parameters = "databasename=" +
                                                                  WebUtil.GetQueryString("databasename");
                        this.MediaDataContext.Parameters = "databasename=" + WebUtil.GetQueryString("databasename");
                    }
                    if (queryString.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!queryString.StartsWith("/sitecore", StringComparison.InvariantCulture))
                        {
                            queryString = FileUtil.MakePath("/sitecore/content", queryString, '/');
                        }
                        if (queryString.EndsWith(".aspx", StringComparison.InvariantCulture))
                        {
                            queryString = StringUtil.Left(queryString, queryString.Length - 5);
                        }
                        this.InternalLinkDataContext.Folder = queryString;
                    }
                    else if (ShortID.IsShortID(queryString))
                    {
                        queryString = ShortID.Parse(queryString).ToID().ToString();
                        Item item = Client.ContentDatabase.GetItem(queryString);
                        if (item != null)
                        {
                            if (!item.Paths.IsMediaItem)
                            {
                                this.InternalLinkDataContext.Folder = queryString;
                            }
                            else
                            {
                                this.MediaDataContext.Folder = queryString;
                                this.MediaTab.Active = true;
                            }
                        }
                    }
                    else
                    {
                        Item item2 = this.InternalLinkDataContext.GetDatabase().GetItem(queryString);
                        if ((item2 != null) && item2.Paths.IsMediaItem)
                            this.MediaTab.Active = true;
                        try
                        {
                            Item item = Client.ContentDatabase.GetItem("/sitecore/content");
                            if (item2.Name == "__Standard Values")
                                InternalLinkDataContext.Folder = item.ID.ToString();
                        }
                        catch (Exception) { }
                    }
                    this.SetUploadButtonAvailability();
                }
            }
        }

        [UsedImplicitly]
        private void OnMediaTreeviewClicked()
        {
            this.SetUploadButtonAvailability();
        }

        protected override void OnOK(object sender, EventArgs args)
        {
            string mediaUrl;
            string displayName;
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            if ((this.Tabs.Active == 0) || (this.Tabs.Active == 2))
            {
                Item selectionItem = this.InternalLinkTreeview.GetSelectionItem();
                if (selectionItem == null)
                {
                    SheerResponse.Alert("Select an item.", new string[0]);
                    return;
                }
                displayName = selectionItem.DisplayName;
                if (selectionItem.Paths.IsMediaItem)
                {
                    mediaUrl = this.GetMediaUrl(selectionItem);
                }
                else
                {
                    if (!selectionItem.Paths.IsContentItem)
                    {
                        SheerResponse.Alert("Select either a content item or a media item.", new string[0]);
                        return;
                    }
                    LinkUrlOptions options = new LinkUrlOptions();
                    mediaUrl = LinkManager.GetDynamicUrl(selectionItem, options);
                }
            }
            else
            {
                MediaItem item2 = this.MediaTreeview.GetSelectionItem();
                if (item2 == null)
                {
                    SheerResponse.Alert("Select a media item.", new string[0]);
                    return;
                }
                displayName = item2.DisplayName;
                mediaUrl = this.GetMediaUrl((Item)item2);
            }
            if (this.Mode == "webedit")
            {
                SheerResponse.SetDialogValue(StringUtil.EscapeJavascriptString(mediaUrl));
                base.OnOK(sender, args);
            }
            else
            {
                SheerResponse.Eval("scClose(" + StringUtil.EscapeJavascriptString(mediaUrl) + "," + StringUtil.EscapeJavascriptString(displayName) + ")");
            }
        }

        private void SetUploadButtonAvailability()
        {
            if (this.Tabs.Active == 1)
            {
                SheerResponse.Eval("document.getElementById('BtnUpload').style.display='';");
                Item selectionItem = this.MediaTreeview.GetSelectionItem();
                if ((selectionItem != null) && selectionItem.Access.CanCreate())
                {
                    SheerResponse.Eval("document.getElementById('BtnUpload').disabled = false;");
                }
                else
                {
                    SheerResponse.Eval("document.getElementById('BtnUpload').disabled = true;");
                }
            }
            else
            {
                SheerResponse.Eval("document.getElementById('BtnUpload').style.display='none';");
            }
        }

        protected Button BtnUpload { get; set; }

        protected string Mode
        {
            get
            {
                string str = StringUtil.GetString(base.ServerProperties["Mode"]);
                if (!string.IsNullOrEmpty(str))
                {
                    return str;
                }
                return "shell";
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                base.ServerProperties["Mode"] = value;
            }
        }
    }
}