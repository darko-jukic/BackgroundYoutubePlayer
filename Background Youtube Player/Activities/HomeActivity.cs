﻿using Android.App;
using Android.Widget;
using Android.OS;
using System.Threading.Tasks;
using YoutubeExtractor;
using Background_Youtube_Player.Resources.model;
using Newtonsoft.Json;
using Android.Views;
using Android.Content;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using SearchView = Android.Support.V7.Widget.SearchView;
using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Background_Youtube_Player.Code.Services;
using Background_Youtube_Player.Code.Helpers;
using Background_Youtube_Player.Code.Data;
using Android.Preferences;

namespace Background_Youtube_Player
{
    [Activity(Label = "Youtube", MainLauncher = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class HomeActivity : BaseActivity
    {
        SearchView songSearchView;
        Toolbar toolbar;
        NavigationView navigationView;

        VideoHelper VideoHelper = new VideoHelper();


        ListView songListView;
        string tag = "https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=50&type=video&key=AIzaSyADs8hX9blKmzfBRkVGxLcQhRdMB80qBTc&q=";

        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Home);
            FindViews();
            HandleEvents();
            await SearchForSong(this, null);
        }

        protected override void OnRestart()
        {
            base.OnRestart();
        }

        protected override void OnStop()
        {
            base.OnStop();
        }

        private void HandleEvents()
        {
            songSearchView.QueryTextSubmit += async (sender, e) => await SearchForSong(sender, e);
            songSearchView.Click += SongSearchView_Click;
        }

        private void SongSearchView_Click(object sender, System.EventArgs e)
        {
            songSearchView.Iconified = false;
        }


        protected void FindViews()
        {
            songListView = FindViewById<ListView>(Resource.Id.resultsListView);
            toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            toolbar.SetTitle(Resource.String.ToolbarTitle);

            drawerLayout = FindViewById<DrawerLayout>(Resource.Id.DrawerLayout);
            navigationView = FindViewById<NavigationView>(Resource.Id.NavigationView);
            navigationView.SetCheckedItem(Resource.Id.nav_home);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_menu);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            navigationView.NavigationItemSelected += NavigationView_NavigationItemSelected;

            songSearchView = FindViewById<SearchView>(Resource.Id.songSearchView);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            toolbar.InflateMenu(Resource.Menu.toolbar_menu);
            toolbar.MenuItemClick += (sender, e) =>
            {
                if (notificationManager != null)
                    notificationManager.CancelAll();
                MediaService.Stop();
            };
            return base.OnPrepareOptionsMenu(menu);
        }

        private async Task SearchForSong(object sender, SearchView.QueryTextSubmitEventArgs e)
        {
            var dialog = DisplayHelper.MakeProgressDialog(this, "Searching...");
            dialog.Show();
            var dataService = new DataService();
            var content = await dataService.GetRequestJson(tag + songSearchView.Query);
            var result = await DeserializeObjectAsync(content);

            dialog.Hide();
            songSearchView.ClearFocus();
            songListView.Adapter = new VideoAdapter(Application.Context, result.items);
            songListView.ItemClick += async (s, events) => await StartPlayingSong(s, events);

            songListView.ItemLongClick += AddToFavorites;

            Window.SetSoftInputMode(SoftInput.StateHidden);
        }

        private Task<Youtube.RootObject> DeserializeObjectAsync(string content)
        {
            return Task.Factory.StartNew(() =>
            {
                var items = JsonConvert.DeserializeObject<Youtube.RootObject>(content);
                return items;
            });
        }

        private void AddToFavorites(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            var adapter = songListView.Adapter as VideoAdapter;
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle("Add to favorites?");
            //alert.SetMessage("Lorem ipsum dolor sit amet, consectetuer adipiscing elit.");
            alert.SetPositiveButton("Yes", (senderAlert, args) => {
                var video = adapter[e.Position];
                FavoriteVideos.List.Add(video);
            });

            alert.SetNegativeButton("No", (senderAlert, args) => {

            });

            Dialog dialog = alert.Create();
            dialog.Show();
        }

        private async Task StartPlayingSong(object sender, AdapterView.ItemClickEventArgs e)
        {
            var dialog = DisplayHelper.MakeProgressDialog(this, "Wait...");
            dialog.Show();
            var adapter = songListView.Adapter as VideoAdapter;
            var id = adapter[e.Position].id.videoId;
            string link = "https://www.youtube.com/watch?v=" + id;

            //PlayVideo(link);

            var video = await VideoHelper.ResolveDownloadUrls(link, this);
            await Play(video);

            dialog.Hide();
        }


        private async Task PlayVideo(string link)
        {
            var intent = new Intent(this, typeof(SongActivity));
            intent.PutExtra("link", link);
            StartActivity(intent);
        }



        public async Task Play(VideoInfo video)
        {
            if (video != null)
            {
                await PlaySong(video);
            }
        }

        private async Task PlaySong(VideoInfo video)
        {
            await MediaService.Start(video.DownloadUrl);
            CreateNotification(video);
        }

    }
}
