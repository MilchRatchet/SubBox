var app = new Vue({
    data: {
        videos: [],
        oldVideos: [],
        channels: [],
        lists: [],
        uniqueList: [],
        tags: [],
        videoListMode: true,
        trashbinMode: false,
        inputListMode: false,
        tagsMode: false,
        inputListModeNumber: 0,
        addPlayList: "",
        addTag: "",
        addTagFilter: "",
        channelMode: false,
        uniqueListMode: false,
        addChannelName: "",
        night: false,
        filter: "SubBox",
        filterImg: "",
        searchFilter: "",
        playlistFilter: "",
        trashbinFilter: "",
        selectedTag: null,
        profileMode: false,
        settingsMode: false,
        settingsTab: 0,
        settings: [],
        profileImage: new Image(),
        inputContext: false,
        selectedInputForContext: null,
        informationMode: false,
        informationContent: [],
        messages: [],
        channelPage: 1,
        maxChannelPage: 10,
        isFirefox: false,
        today: new Date(),
        filteredLength: "0:00",
        totalLength: "0:00",
        newestVersion: "1.5.1",
        messageRunningId: 0,
        confirmationMessage: "",
        confirmationResult: false,
        confirmationDone: false,
        viewPortTop: 10,
        viewPortBottom: 0,
        viewPortAnchor: 0,
    },
    computed: {
        filteredVideos: function () {
            if (this.filter === "SubBox" && this.searchFilter === "" && this.selectedTag == null) {
                return this.videos;
            }

            searchFilterUp = this.searchFilter.toUpperCase();

            if (this.filter === "SubBox" && this.selectedTag == null) {
                return this.videos.filter(function (u) {
                    return (u.title + u.description + u.channelTitle).toUpperCase().includes(searchFilterUp);
                });
            }
            if (this.filter === "SubBox") {
                return this.videos.filter(function (u) {
                    return ((u.title + u.description + u.channelTitle).toUpperCase().includes(searchFilterUp) && app.selectedTag.filterList.some(function(f) {
                        return (u.title + u.description + u.channelTitle).toUpperCase().includes(f.toUpperCase());
                    }));
                });
            }
            if (this.selectedTag == null) {
                return this.videos.filter(function (u) {
                    return (u.channelTitle == app.filter && (u.title + u.description + u.channelTitle).toUpperCase().includes(searchFilterUp));
                });
            }
            return this.videos.filter(function (u) {
                return (u.channelTitle == app.filter && (u.title + u.description + u.channelTitle).toUpperCase().includes(searchFilterUp) && app.selectedTag.filterList.some(function(f) {
                    return (u.title + u.description + u.channelTitle).toUpperCase().includes(f.toUpperCase());
                }));
            });
        },
        filteredPlaylist: function () {
            if (this.playlistFilter === "") return this.uniqueList;

            playlistFilterUp = this.playlistFilter.toUpperCase();

            return this.uniqueList.filter(function (u) {
                return (u.title + u.description + u.channelTitle).toUpperCase().includes(playlistFilterUp);
            });
        }
    },
    functions: {

    },
    methods: {
        async update() {
            var result = await fetch("/api/values/videos");

            this.videos = await result.json();

            this.videos.forEach(v => v.publishedAt = new Date(v.publishedAt));

            this.checkDownloadStatus();
        },
        async checkDownloadStatus() {
            var result = await fetch("/api/values/localvideos");

            locals = await result.json();

            this.videos.forEach(function (v) {
                Vue.set(v, 'dlstatus', (locals.some((l) => l.Key == v.id)) ? 2 : 0);
            });
        },
        async checkDownloadStatusUniqueVideo(video) {
            var result = await fetch("/api/values/localvideos");

            locals = await result.json();

            Vue.set(video, 'dlstatus', (locals.some((l) => l.Key == video.id)) ? 2 : 0);
        },
        async listUpdate() {
            var result = await fetch("/api/values/lists");

            this.lists = await result.json();

            this.initializeSmartListLoading();
        },
        async tagsUpdate() {
            var result = await fetch("/api/values/tags");

            this.tags = await result.json();

            this.tags.forEach(function (t) {
                if (t.filter == "") {
                    t.filterList = [];
                } else {
                    t.filterList = t.filter.split("§");
                }
                t.inEdit = false;
            });

            this.tags.sort();
        },
        async getSettings() {
            var result = await fetch("/api/values/settings");

            this.settings = await result.json();

            document.documentElement.style.setProperty('--mainColor', "#" + this.settings.Color);

            document.getElementById("colorPicker").jscolor.fromString(this.settings.Color);

            this.setSecondColor();

            this.setBodyColor();

            this.updateProfileImage();
        },
        updateProfileImage() {
            if (this.settings.PicOfTheDayUrl != this.profileImage.src) {
                this.profileImage.src = this.settings.PicOfTheDayUrl;
            }
        },
        async saveSettings() {
            const output = JSON.stringify(this.settings);

            await fetch("/api/values/settings/save", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: output
            });
        },
        async getChannels() {
            var waiter = fetch("/api/values/channels");

            var result = await waiter;

            this.channels = await result.json();

            this.unlockAllChannels();

            if (this.filter === "SubBox") {
                return;
            }

            this.channels.find(function (e) {
                return e.displayname === app.filter;
            }).locked = true;
        },
        async showChannels() {
            this.channelMode = !this.channelMode;

            if (this.channelMode) {
                var waiter = fetch("/api/values/channels");

                if (this.tagsMode) {
                    await this.showTags();
                }
    
                if (this.inputListMode) {
                    this.showListInput();
                }
    
                if (this.trashbinMode) {
                    await this.showTrashbin();
                }
    
                var result = await waiter;
    
                this.channels = await result.json();
    
                this.calcChannelStats();
    
                this.maxChannelPage = Math.ceil(this.channels.length / this.settings.ChannelsPerPage);
    
                if (this.channelPage > this.maxChannelPage) this.channelPage = 1;
    
                this.unlockAllChannels();
    
                if (this.filter === "SubBox") {
                    return;
                }
    
                this.channels.find(function (e) {
                    return e.displayname === app.filter;
                }).locked = true;
            }
        },
        async showListInput() {
            if (this.inputListMode) {
                this.inputListMode = false;

                return;
            }

            if (this.channelMode) {
                await this.showChannels();
            }

            if (this.tagsMode) {
                await this.showTags();
            }

            if (this.trashbinMode) {
                await this.showTrashbin();
            }

            this.addPlayList = "";

            this.addPlayListUnique = "";

            this.inputListMode = !this.inputListMode;
        },
        async showUniqueList(number) {
            if (number > 0) {
                this.uniqueList = [];

                var waiter = fetch("/api/values/list/all/" + number);

                this.inputListModeNumber = number;

                this.channelMode = false;

                var result = await waiter;

                this.uniqueList = await result.json();

                this.uniqueListMode = true;

                this.videoListMode = false;
            }
        },
        async showTrashbin() {
            if (this.trashbinMode) {
                this.trashbinMode = false;

                this.oldVideos = [];

                return;
            }

            this.trashbinFilter = "";

            if (this.channelMode) {
                await this.showChannels();
            }

            if (this.tagsMode) {
                await this.showTags();
            }

            if (this.inputListMode) {
                await this.showListInput();
            }

            var waiter = fetch("api/values/videos/old");

            var result = await waiter;

            this.oldVideos = await result.json();

            this.oldVideos.forEach(v => v.publishedAt = new Date(v.publishedAt));

            this.trashbinMode = true;
        },
        showSettings() {

            if (this.profileMode) 
            {
                this.showProfile();
            }
            else 
            {
                if (this.settingsMode) 
                {
                    this.saveSettings();
                } 
                else 
                {
                    this.getSettings();
                }
            }

            this.settingsTab = 0;
            
            this.settingsMode = !this.settingsMode;
        },
        showProfile() {

            if (this.settingsMode) 
            {               
                this.showSettings();
            }
            else 
            { 
                if (this.profileMode) 
                {
                    this.saveSettings();
                } 
                else 
                {
                    this.getSettings();
                }
            }

            this.profileMode = !this.profileMode;
        },
        async showTags() {
            if (this.tagsMode) {
                this.tagsMode = false;

                this.tags.forEach(function (t) {
                    t.inEdit = false;
                });

                return;
            }

            if (this.channels.length == 0) {
                var waiter = fetch("/api/values/channels");

                var result = await waiter;

                this.channels = await result.json();
            }

            if (this.channelMode) {
                await this.showChannels();
            }

            if (this.inputListMode) {
                await this.showListInput();
            }

            if (this.trashbinMode) {
                await this.showTrashbin();
            }

            this.tagsMode = true;
        },
        async refresh() {
            await fetch("/api/values/refresh", { method: "POST", });

            this.update();
        },
        setColor() {
            this.settings.Color = document.getElementById('colorPicker').value;

            document.documentElement.style.setProperty('--mainColor', "#" + this.settings.Color);

            this.setSecondColor();

            this.changes = true;
        },
        resetColor() {
            this.settings.Color = "DB4437";

            document.getElementById("colorPicker").jscolor.fromString(this.settings.Color);

            document.documentElement.style.setProperty('--mainColor', "#" + this.settings.Color);

            this.setSecondColor();

            this.changes = true;
        },
        async submitTag() {
            if (this.addTag != "") {
                this.addTag = this.addTag.trim();

                await fetch("api/values/tags/add/" + this.addTag, { method: "POST" });

                this.addTag = "";

                this.tagsUpdate();
            }
        },
        async submitChannel() {
            if (this.addChannelName != "") {
                this.addChannelName = this.addChannelName.trim();

                const requestString = this.addChannelName;

                this.addChannelName = "";

                var waiter = fetch("/api/values/channel/" + requestString, { method: "POST" });

                await waiter;

                this.update();

                if (this.channelMode) {
                    this.channelMode = false;

                    await this.showChannels();
                }

                var updater = setInterval(async function () {
                    var result = await fetch("/api/values/status/channelResult/" + requestString);

                    var status = await result.json();

                    if (status.kind === "channelResult") {

                        status.value = status.value === 'true';

                        if (!status.value && app.settings.ChannelAddedNotification) {
                            const messageId = app.messageRunningId++;

                            app.messages.push({
                                "id": messageId,
                                "title": "Channel could not be added",
                                "subtitle": "Check out the introduction to look up how to add channels",
                                "thumbUrl": "media/LogoRed.png",
                                "text": "Input: " + requestString,
                                "event": "return;"
                            });

                            setTimeout(function () { const index = app.messages.findIndex(m => m.id === messageId); app.messages.splice(index, 1); }, 10000);

                            clearInterval(updater);

                            return;
                        }

                        const channel = app.channels.find(c => (c.id === requestString || c.username === requestString));

                        if (channel !== undefined && app.settings.ChannelAddedNotification) {
                            const messageId = app.messageRunningId++;

                            app.messages.push({
                                "id": messageId,
                                "title": channel.displayname + " was added",
                                "subtitle": "Username: " + channel.username,
                                "thumbUrl": 'channelPictures/' + channel.id + '.jpg',
                                "text": "Id: " + channel.id,
                                "event": "app.filter = '" + channel.displayname + "'; app.lockChannel('" + channel.id + "');"
                            });

                            setTimeout(function () { const index = app.messages.findIndex(m => m.id === messageId); app.messages.splice(index, 1); }, 10000);
                        }

                        clearInterval(updater);
                    }
                }, 1000);

            }
        },
        async submitPlaylist() {
            if (this.addPlayList != "") {
                this.addPlayList = this.addPlayList.trim();

                this.inputListMode = false;

                await fetch("/api/values/list/add/" + this.addPlayList + "/" + this.inputListModeNumber, { method: "POST" });

                this.addPlayList = "";

                this.listUpdate();

                if (this.uniqueListMode) {
                    this.showUniqueList(this.inputListModeNumber);
                }
            }
        },
        openVideo(link) {
            let handle = window.open("https://www.youtube.com/watch?v=" + link, "_blank");

            handle.blur();

            window.focus();
        },
        async downloadVideo(video) {
            fetch("/api/values/download/" + video.id, { method: "POST" });

            Vue.set(video, 'dlprogress', "0");

            Vue.set(video, 'dlstatus', 1);

            const bar = document.getElementById("video" + video.id);

            var progressUpdater = setInterval(async function () {
                var result = await fetch("/api/values/status/downloadProgress/" + video.id);

                var progress = await result.json();

                if (progress.kind === "downloadProgress") {
                    Vue.set(video, 'dlprogress', progress.value);

                    bar.style.setProperty("background", "linear-gradient(to left, #232323 0 " + (100 - progress.value) + "%, #8f54f1 " + (100 - progress.value) + "%)");
                }
            }, 100);

            var updater = setInterval(async function () {
                var result = await fetch("/api/values/status/downloadResult/" + video.id);

                var status = await result.json();

                if (status.kind === "downloadResult") {

                    status.value = status.value === 'true';

                    Vue.set(video, 'dlstatus', ((status.value) ? 2 : 0));

                    if (app.settings.DownloadFinishedNotification) {
                        const messageId = app.messageRunningId++;

                        app.messages.push({
                            "id": messageId,
                            "title": video.title,
                            "subtitle": video.channelTitle,
                            "thumbUrl": video.thumbnailUrl,
                            "text": ((status.value) ? "Download Finished" : "Download Failed"),
                            "event": ((status.value) ? "window.location.replace('http://localhost:5000/localbrowser.html?id=" + video.id + "');" : "return;")
                        });

                        setTimeout(function () { const index = app.messages.findIndex(m => m.id === messageId); app.messages.splice(index, 1); }, 10000);
                    }

                    clearInterval(progressUpdater);

                    clearInterval(updater);
                }
            }, 1000);
        },
        async deleteVideo(video) {
            if (this.uniqueListMode) {
                var waiter = fetch("/api/values/video/" + video.id, { method: "DELETE" });

                var index = this.uniqueList.indexOf(video);

                this.uniqueList.splice(index, 1);

                await waiter;

                this.listUpdate();
            } else {
                fetch("/api/values/video/" + video.id, { method: "DELETE" });

                if (this.trashbinMode) this.showTrashbin();

                var index = this.videos.indexOf(video);

                this.videos.splice(index, 1);
            }
        },
        async deleteFilteredVideos() {
            if (!(await this.getConfirmation("Delete all Filtered Videos?"))) return;

            if (this.filter === "SubBox" && this.searchFilter === "" && this.selectedTag === null) return;

            var count = 0;

            const duration = this.updateVideosLength(this.filteredVideos);

            var firstThumb = null;

            this.filteredVideos.forEach(function (item) {
                fetch("/api/values/video/" + item.id, { method: "DELETE" });

                var index = app.videos.indexOf(item);

                if (firstThumb === null) firstThumb = item.thumbnailUrl;

                app.videos.splice(index, 1);

                count++;
            });

            this.resetAllFilters();

            if (this.settings.VideosDeletionNotification) {
                const messageId = app.messageRunningId++;

                app.messages.push({
                    "id": messageId,
                    "title": "Filtered Videos Deleted",
                    "subtitle": count + " Videos",
                    "thumbUrl": (firstThumb === null) ? "media/LogoRed.png" : firstThumb,
                    "text": "Length of Deleted Videos " + duration,
                    "event": "return;"
                });

                setTimeout(function () { const index = app.messages.findIndex(m => m.id === messageId); app.messages.splice(index, 1); }, 10000);
            }
        },
        async deleteFilteredPlaylistVideos() {   
            if (!(await this.getConfirmation("Delete all Filtered Videos?"))) return;

            var count = 0;

            const duration = this.updateVideosLength(this.filteredPlaylist);

            var firstThumb = null;

            this.filteredPlaylist.forEach(async function (item) {
                var waiter = fetch("/api/values/video/" + item.id, { method: "DELETE" });

                var index = app.uniqueList.indexOf(item);

                if (firstThumb === null) firstThumb = item.thumbnailUrl;

                app.uniqueList.splice(index, 1);

                count++;

                await waiter;
            });

            if (this.settings.VideosDeletionNotification) {
                const messageId = app.messageRunningId++;

                app.messages.push({
                    "id": messageId,
                    "title": "Filtered Videos Deleted",
                    "subtitle": count + " Videos",
                    "thumbUrl": (firstThumb === null) ? "media/LogoRed.png" : firstThumb,
                    "text": "Length of Deleted Videos " + duration,
                    "event": "return;"
                });

                setTimeout(function () { const index = app.messages.findIndex(m => m.id === messageId); app.messages.splice(index, 1); }, 10000);
            }

            this.playlistFilter = "";
        },
        async deleteNotFilteredPlaylistVideos() {
            if (!(await this.getConfirmation("Only Keep Filtered Videos?"))) return;

            const NotFilteredPlaylist = this.uniqueList.filter(function (u) {
                return !app.filteredPlaylist.includes(u);
            });

            var count = 0;

            var firstThumb = null;

            NotFilteredPlaylist.forEach(async function (item) {
                var waiter = fetch("/api/values/video/" + item.id, { method: "DELETE" });

                var index = app.uniqueList.indexOf(item);

                if (firstThumb === null) firstThumb = item.thumbnailUrl;

                app.uniqueList.splice(index, 1);

                await waiter;

                count++;
            });

            if (this.settings.VideosDeletionNotification) {
                const messageId = app.messageRunningId++;

                app.messages.push({
                    "id": messageId,
                    "title": "Filtered Videos Deleted",
                    "subtitle": count + " Videos",
                    "thumbUrl": (firstThumb === null) ? "media/LogoRed.png" : firstThumb,
                    "text": "",
                    "event": "return;"
                });

                setTimeout(function () { const index = app.messages.findIndex(m => m.id === messageId); app.messages.splice(index, 1); }, 10000);
            }

            this.playlistFilter = "";
        },
        async deleteCompletePlaylist() {
            if (!(await this.getConfirmation("Delete Playlist?"))) return;

            var count = this.uniqueList.length;

            const duration = this.updateVideosLength(this.uniqueList);

            var firstThumb = this.uniqueList.find(item => true);

            if (firstThumb !== undefined) firstThumb = firstThumb.thumbnailUrl;

            this.lists.forEach(function (item) {
                if (item.list === app.inputListModeNumber) {
                    const index = app.lists.indexOf(item);

                    app.lists.splice(index, 1);
                }
            });

            this.closeUniqueList();

            this.uniqueList.forEach(async function (item) {
                await fetch("/api/values/video/" + item.id, { method: "DELETE" });
            });    

            if (this.settings.VideosDeletionNotification) {
                const messageId = app.messageRunningId++;

                app.messages.push({
                    "id": messageId,
                    "title": "Playlist Deleted",
                    "subtitle": count + " Videos",
                    "thumbUrl": (firstThumb === undefined) ? "media/LogoRed.png" : firstThumb,
                    "text": "Length of Deleted Videos " + duration,
                    "event": "return;"
                });

                setTimeout(function () { const index = app.messages.findIndex(m => m.id === messageId); app.messages.splice(index, 1); }, 10000);
            }
        },
        async reactivateVideo(video) {
            fetch("/api/values/video/" + video.id, { method: "POST" });

            var index = this.oldVideos.indexOf(video);

            this.oldVideos.splice(index, 1);

            index = 0;

            this.videos.forEach(v => {
                if (v.publishedAt > video.publishedAt) {
                    index++;
                }
            });

            this.videos.splice(index, 0, video);

            this.checkDownloadStatusUniqueVideo(video);
        },
        openChannel(link) {
            window.open("https://www.youtube.com/user/" + link, "_blank");
        },
        calcChannelStats() {
            this.channels.forEach(function (c) {
                channelVideos = app.videos.filter(function (v) {
                    return v.channelTitle == c.displayname;
                });

                c.videoCount = channelVideos.length;

                c.videoLength = app.updateVideosLength(channelVideos);
            });
        },
        unlockAllChannels() {
            this.channels.forEach(function (c) {
                c.locked = false;
            });
        },
        lockChannel(id) {
            const ch = this.channels.find(c => c.id === id);

            if (ch === undefined) return;

            if (ch.locked) {
                ch.locked = false;
                return;
            }

            this.unlockAllChannels();

            ch.locked = true;
        },
        closeUniqueList() {
            this.inputListModeNumber = 0;

            this.videoListMode = true;

            this.uniqueListMode = false;

            this.playlistFilter = "";
        },
        async switchDesign() {
            this.settings.NightMode = !this.settings.NightMode;

            this.setBodyColor();

            await fetch("/api/values/settings/NIGHT/" + null, { method: "POST" });

            fetch("/api/values/settings/save", { method: "POST" });
        },
        async syncChannelPictures() {
            await fetch("/api/values/settings/syncChannelPictures", {method: "POST"});

            location.reload();
        },
        async togglePlaylistDisplay(list) {
            Vue.set(this.settings.DisplayPlaylists, list-1, !this.settings.DisplayPlaylists[list-1]);

            await this.saveSettings();
        },
        setBodyColor() {
            if (this.settings.NightMode) {
                document.body.setAttribute('bgcolor', '#131313');
            } else {
                document.body.setAttribute('bgcolor', '#fafafa');
            }
        },
        async deleteChannel(channel) {
            if (!(await this.getConfirmation("Remove " + channel.displayname + "?"))) return;

            if (channel.displayname == this.filter) {
                this.filter = "SubBox";
            }

            var waiter = fetch("/api/values/channel/" + channel.id, { method: "DELETE" });

            var index = this.channels.indexOf(channel);

            this.channels.splice(index, 1);

            await waiter;

            this.update();
        },
        async next(list) {
            await fetch("api/values/list/next/" + list, { method: "POST" });

            this.listUpdate();
        },
        async prev(list) {
            await fetch("api/values/list/previous/" + list, { method: "POST" });

            this.listUpdate();
        },
        setSecondColor() {
            var r = parseInt(this.settings.Color.substr(0, 2), 16);
            var g = parseInt(this.settings.Color.substr(2, 2), 16);
            var b = parseInt(this.settings.Color.substr(4, 2), 16);
            var yiq = ((r * 299) + (g * 587) + (b * 114)) / 1000;
            if (yiq >= 200) {
                document.documentElement.style.setProperty('--secColor', 'black');
            } else {
                document.documentElement.style.setProperty('--secColor', 'white');
            }
        },
        async invertPlaylist() {
            await fetch("api/values/list/invert/" + this.inputListModeNumber, { method: "POST" });

            this.uniqueList = [];

            var result = await fetch("/api/values/list/all/" + this.inputListModeNumber);

            this.uniqueList = await result.json();
        },
        async jumpToPlaylistItem(target) {
            const index = target.index;

            await fetch("api/values/list/jump/" + this.inputListModeNumber + "/" + index, { method: "POST" });

            this.uniqueList.forEach(v => v.index -= index);

            this.listUpdate();
        },
        async filterByChannelFromVideoList(id) {
            if (this.channels.length == 0) await this.getChannels();

            channel = this.channels.find(ch => ch.id == id);

            this.setFilter(channel);
        },
        setFilter(ch) {
            if (ch.displayname == this.filter) {
                this.filter = "SubBox";

                this.filterImg = "";
            } else {
                this.filter = ch.displayname;

                this.filterImg = "channelPictures/" + ch.id + ".jpg";
            }

            this.lockChannel(ch.id);

            this.initializeSmartListLoading();
        },
        selectTag(tag) {
            if (this.selectedTag == null) {
                this.selectedTag = tag;

                this.initializeSmartListLoading();

                return;
            }

            if (this.selectedTag.name == tag.name) {
                this.selectedTag = null;

                this.initializeSmartListLoading();

                return;
            }

            this.selectedTag = tag;

            this.initializeSmartListLoading();
        },
        async deleteTag(tag) {
            if (!(await this.getConfirmation("Remove Tag " + tag.name + "?"))) return;

            if (this.selectedTag != null && this.selectedTag.name == tag.name) {
                this.selectedTag = null;

                this.initializeSmartListLoading();
            }

            fetch("/api/values/tags/delete/" + tag.name, { method: "DELETE" });

            var index = this.tags.indexOf(tag);

            this.tags.splice(index, 1);
        },
        editTag(tag) {
            if (tag.inEdit) {
                this.saveFilters(tag);
                return;
            }

            this.tags.forEach(function (t) {
                t.inEdit = t.name == tag.name;
            });

            this.tags.sort();
        },
        async addFilterToTag(tag) {
            if (this.addTagFilter != "") {
                tag.filterList.push(this.addTagFilter);

                this.addTagFilter = "";

                var forceUpdateTag = this.selectedTag;

                this.selectedTag = null;

                this.selectedTag = forceUpdateTag;

                tag.filter = tag.filterList.join('§');

                fetch("/api/values/tags/set/" + tag.name + "/§" + tag.filter, { method: "POST" });

            }     
        },
        async saveFilters(tag) {
            var forceUpdateTag = this.selectedTag;

            this.selectedTag = null;

            this.selectedTag = forceUpdateTag;

            tag.filter = tag.filterList.join('§');

            fetch("/api/values/tags/set/" + tag.name + "/§" + tag.filter, { method: "POST" });

            this.tags.forEach(function (t) {
                t.inEdit = false;
            });

            this.tags.sort();
        },
        async deleteFilter(tag, filter) {
            var index = tag.filterList.indexOf(filter);

            this.$delete(tag.filterList, index);

            var forceUpdateTag = this.selectedTag;

            this.selectedTag = null;

            this.selectedTag = forceUpdateTag;

            tag.filter = tag.filterList.join('§');

            fetch("/api/values/tags/set/" + tag.name + "/§" + tag.filter, { method: "POST" });

            this.tags.sort();
        },
        goToTag() {
            this.selectedTag.inEdit = true;

            this.showTags();
        },
        resetAllFilters() {
            this.filter = "SubBox";

            this.filterImg = "";

            this.selectedTag = null;

            this.searchFilter = "";

            this.unlockAllChannels();

            this.initializeSmartListLoading();
        },
        async actOnClipboard(command) {
            if (command === 'paste') {
                if (this.isFirefox) return;

                var event = new Event("input", { bubbles: true });

                this.selectedInputForContext.value = await navigator.clipboard.readText();

                this.selectedInputForContext.dispatchEvent(event);

                return;
            }

            if (command === 'delete') {
                var event = new Event("input", { bubbles: true });

                this.selectedInputForContext.value = "";

                this.selectedInputForContext.dispatchEvent(event);

                return;
            }

            this.selectedInputForContext.focus();

            this.selectedInputForContext.select();

            document.execCommand(command);
        },
        async toggleInformationMode() {
            this.informationMode = !this.informationMode;

            if (this.informationMode) {
                var waiter = fetch("/api/values/information");

                this.totalLength = this.updateVideosLength(this.videos);

                if (this.uniqueListMode) {
                    this.filteredLength = this.updateVideosLength(this.uniqueList);
                } else {
                    this.filteredLength = this.updateVideosLength(this.filteredVideos);
                }

                var result = await waiter;

                this.informationContent = await result.json();
            }
        },
        execEvent(message) {
            const func = new Function(message.event);

            func();
        },
        async getConfirmation(message) {
            if (!this.settings.ConfirmWindow) {
                return new Promise((resolve, reject) => {
                    resolve(true);
                });
            }

            this.confirmationMessage = message;

            const promise = new Promise((resolve, reject) => {

                const interval = setInterval(function () {
                    if (!app.confirmationDone) return;

                    app.confirmationDone = false;

                    app.confirmationMessage = "";

                    clearInterval(interval);

                    resolve(app.confirmationResult);
                }, 100);
            });

            return promise;
        },
        updateVideosLength(videoCollection) {
            let sec = 0;

            let min = 0;

            let h = 0;

            videoCollection.forEach((v) => {
                const pieces = v.duration.split(':');

                const len = pieces.length;

                sec += parseInt(pieces[len - 1], 10);

                min += parseInt(pieces[len - 2], 10);

                if (len === 3) {
                    h += parseInt(pieces[0], 10);
                }
            });

            min += Math.floor(sec / 60);

            h += Math.floor(min / 60);

            sec = sec % 60;

            min = min % 60;

            if (h > 0) {
                if (min < 10) min = "0" + min;

                if (sec < 10) sec = "0" + sec;

                return h + ":" + min + ":" + sec;
            } else {
                if (sec < 10) sec = "0" + sec;

                return min + ":" + sec;
            }
        },
        async getNewestVersion() {
            var waiter = await fetch("/api/values/information");

            this.informationContent = await waiter.json();

            var result = await fetch("https://api.github.com/repos/MilchRatchet/SubBox/releases/latest");

            const latest = await result.json();

            this.newestVersion = latest.tag_name;

            this.newestVersion = this.newestVersion.substring(1, this.newestVersion.length);

            if (this.newestVersion !== this.informationContent[0] && this.settings.NewVersionNotification) {
                const messageId = this.messageRunningId++;

                this.messages.push({
                    "id" : messageId,
                    "title": "Newer Version is available",
                    "subtitle": "Current: v" + this.informationContent[0],
                    "thumbUrl": "media/LogoRed.png",
                    "text": "New: v" + this.newestVersion,
                    "event": "window.open('https://github.com/MilchRatchet/SubBox/releases/latest','_blank');"
                });

                setTimeout(function () { const index = app.messages.findIndex(m => m.id === messageId); app.messages.splice(index, 1); }, 10000);
            }
        },
        specialDayMessages() {
            const date = new Date();

            const day = date.getDate();

            const month = date.getMonth();

            if (day === 31 && month === 9) {
                const messageId = this.messageRunningId++;

                this.messages.push({
                    "id": messageId,
                    "title": "If that was you on the phone and you on the bus...",
                    "subtitle": "   ...then who was flickering the lights?",
                    "thumbUrl": "media/3110.png",
                    "text": "Nosferatu " + '\uD83D\uDC7B',
                    "event": "const index = app.messages.findIndex(m => m.id == '" + messageId + "'); app.messages.splice(index, 1); if (!app.night) app.switchDesign();"
                });
            }

            if (day > 23 && day < 27 && month === 11) {
                const messageId = this.messageRunningId++;

                this.messages.push({
                    "id": messageId,
                    "title": "Merry Christmas",
                    "subtitle": '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744',
                    "thumbUrl": "media/2412.png",
                    "text": '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744' + '\u2744',
                    "event": "const index = app.messages.findIndex(m => m.id == '" + messageId + "'); app.messages.splice(index, 1);"
                });
            }

            if (day === 31 && month === 11) {
                const messageId = this.messageRunningId++;

                this.messages.push({
                    "id": messageId,
                    "title": "Happy New Year",
                    "subtitle": "Looking forward to " + (date.getFullYear() + 1) + ".",
                    "thumbUrl": "media/3112.png",
                    "text": '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80',
                    "event": "const index = app.messages.findIndex(m => m.id == '" + messageId + "'); app.messages.splice(index, 1);"
                });
            }

            if (day === 9 && month === 9) {
                const messageId = this.messageRunningId++;

                this.messages.push({
                    "id": messageId,
                    "title": "Happy Leif Erikson Day.",
                    "subtitle": "hinga-dinga-durgen",
                    "thumbUrl": "media/0910.png",
                    "text": "My favorite holiday, after April Fool's Day.",
                    "event": "const index = app.messages.findIndex(m => m.id == '" + messageId + "'); app.messages.splice(index, 1);"
                });
            }

            if (day === 5 && month === 9) {
                const messageId = this.messageRunningId++;

                this.messages.push({
                    "id": messageId,
                    "title": "SubBox is celebrating its Anniversary.",
                    "subtitle": "Thanks for using SubBox.",
                    "thumbUrl": "media/LogoRed.png",
                    "text": '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89',
                    "event": "const index = app.messages.findIndex(m => m.id == '" + messageId + "'); app.messages.splice(index, 1);"
                });
            }

            if (day === 1 && month === 3) {
                const messageId = this.messageRunningId++;

                this.messages.push({
                    "id": messageId,
                    "title": "It is April Fool's Day.",
                    "subtitle": "Don't trust everything you read today.",
                    "thumbUrl": "media/0104.png",
                    "text": "You never know who wants to fool you" + '\uD83D\uDC40',
                    "event": "const index = app.messages.findIndex(m => m.id == '" + messageId + "'); app.messages.splice(index, 1); app.openVideo('dQw4w9WgXcQ');"
                });
            }

            if (day === 14 && month === 1) {
                const messageId = this.messageRunningId++;

                this.messages.push({
                    "id": messageId,
                    "title": "It is Valentins Day.",
                    "subtitle": "Did you know that it is Youtube's Birthday?",
                    "thumbUrl": "media/1402.png",
                    "text": "Youtube just turned " + (date.getFullYear() - 2005) + " years.",
                    "event": "const index = app.messages.findIndex(m => m.id == '" + messageId + "'); app.messages.splice(index, 1);"
                });
            }
        },
        initializeSmartListLoading() {
            var mainList = document.querySelector('#mainList');

            var mainVideoList = document.querySelector('#mainVideoList');

            mainList.scrollTop = 0;

            if (this.settings.SmartListLoading) {
                app.viewPortAnchor = Math.floor(mainList.scrollTop / 215) * 215;

                app.viewPortTop = Math.floor((mainList.scrollTop + window.innerHeight) / 215) + 1;

                app.viewPortBottom = Math.max(Math.floor((mainList.scrollTop - app.lists.length * 165) / 215) - 2, 0);

                mainVideoList.style.paddingTop = (app.viewPortBottom * 215) + "px";

                mainVideoList.style.height = ((app.filteredVideos.length - app.viewPortBottom) * 215) + "px";
            } else {
                app.viewPortBottom = 0;

                app.viewPortTop = Number.MAX_SAFE_INTEGER;

                mainVideoList.style.paddingTop = "0";

                mainVideoList.style.height = "auto";
            }
        },
        async closeSubBox() {
            if (!(await this.getConfirmation("Close SubBox?"))) return;

            await this.saveSettings();

            fetch("/api/values/close", { method: "POST" });

            setTimeout(() => window.location.replace("https://www.youtube.com/"), 100);
        },
    },
    el: "#app",
    async mounted() {

        this.isFirefox = typeof InstallTrigger !== 'undefined';

        this.getNewestVersion();

        this.specialDayMessages();

        var mainList = document.querySelector('#mainList');

        var mainVideoList = document.querySelector('#mainVideoList');

        var updateSmartListLoading = function(event) {
            if (!app.settings.SmartListLoading) return;

            if (Math.abs(app.viewPortAnchor - mainList.scrollTop) < 215) return;

            app.viewPortAnchor = Math.floor(mainList.scrollTop / 215) * 215;

            app.viewPortTop = Math.floor((mainList.scrollTop + window.innerHeight) / 215) + 1;

            app.viewPortBottom = Math.max(Math.floor((mainList.scrollTop - app.lists.length * 165) / 215) - 2, 0);

            mainVideoList.style.paddingTop = (app.viewPortBottom * 215) + "px";

            mainVideoList.style.height = ((app.filteredVideos.length - app.viewPortBottom) * 215) + "px";

            app.videos.push("update");

            app.videos.pop();
        }

        mainList.addEventListener("scroll", updateSmartListLoading);

        window.addEventListener("resize", updateSmartListLoading);

        const page = document.getElementById("app");

        page.addEventListener("contextmenu", function (event) {
            event.preventDefault();

            if (app.informationMode) return;

            app.inputContext = false;

            event = event || window.event;

            const target = event.target || event.srcElement;

            if (target.nodeName === "INPUT" && target.className !== "search" && target.type.toLowerCase() === 'text') {
                app.inputContext = true;

                app.selectedInputForContext = target;
            }

            var ctxMenu = document.getElementById("ctxMenu");

            ctxMenu.style.display = "block";

            if (event.clientX > window.innerWidth - ctxMenu.offsetWidth) {
                ctxMenu.style.left = (window.innerWidth - ctxMenu.offsetWidth - 10) + "px";
            } else {
                ctxMenu.style.left = (event.clientX + 1) + "px";
            }

            if (event.clientY > window.innerHeight - ctxMenu.offsetHeight) {
                ctxMenu.style.top = (window.innerHeight - ctxMenu.offsetHeight - 1) + "px";
            } else {
                ctxMenu.style.top = (event.clientY + 1) + "px";
            }

        }, false);

        var setOv = document.querySelector('#settingsUI');

        var proOv = document.querySelector('#profileUI');

        var chOv = document.querySelector('#channelUI');

        var tagsOv = document.querySelector('#tagsUI');

        var infOv = document.querySelector('#informationOverlay');

        var ctxOv = document.querySelector('#ctxMenu');

        var aplOv = document.querySelector('#addPlaylistUI');

        var tbOv = document.querySelector('#trashbinUI');

        page.addEventListener("click", function (event) {

            event = event || window.event;

            const target = event.target || event.srcElement;

            app.inputContext = false;

            var ctxMenu = document.getElementById("ctxMenu");

            ctxMenu.style.display = "";

            ctxMenu.style.left = "";

            ctxMenu.style.top = "";

            if (app.confirmationMessage != '') return;

            if (app.settingsMode && target.nodeName !== "BUTTON") {
                if (!setOv.contains(target)) {
                    app.showSettings();
                }
            }

            if (app.profileMode && target.nodeName !== "BUTTON") {
                if (!proOv.contains(target)) {
                    app.showProfile();
                }
            }

            if (app.channelMode && target.nodeName !== "BUTTON") {
                if (!chOv.contains(target)) {
                    app.showChannels();
                }
            }

            if (app.tagsMode && target.nodeName !== "BUTTON") {
                if (target.className != "selectedTag") {
                    if (!tagsOv.contains(target)) {
                        app.showTags();
                    }
                }
            }

            if (app.informationMode && !ctxOv.contains(target)) {
                if (!infOv.contains(target)) {
                    app.toggleInformationMode();
                }
            }

            if (app.inputListMode && target.nodeName !== "BUTTON") {
                if (!aplOv.contains(target)) {
                    app.showListInput();
                }
            }

            if (app.trashbinMode && target.nodeName !== "BUTTON") {
                if (!tbOv.contains(target)) {
                    if (!target.className.includes("trashbin")) {
                        app.showTrashbin();
                    }
                }
            }

        }, false);

        document.addEventListener("keydown", function (event) {
            if (document.activeElement.nodeName === "INPUT" || document.activeElement.nodeName === "TEXTAREA" ) return;

            if (event.altKey) return;

            if (event.ctrlKey) return;

            if (event.shiftKey) return;

            if (event.code === "Escape") return;

            if (app.videoListMode) {
                document.getElementById("search").focus();
            } else if (app.uniqueListMode) {
                document.getElementById("playlistSearch").focus();
            }
        }, false);

        document.addEventListener("keyup", function (event) {
            if (event.code === "Escape") {
                if (app.uniqueListMode) {
                    app.closeUniqueList();
                }
            }
        }, false);

        /*document.body.onmousedown = function (e) { if (e.button === 1) return false; }*/

        var result = await fetch("/api/values/first");

        var first = await result.json();

        if (first) {
            window.location.replace("http://localhost:5000/tutorial.html");
        }

        await this.getSettings();

        await this.listUpdate();

        await this.update();

        this.tagsUpdate();

        if (this.settings.SmartListLoading) {
            this.viewPortTop = Math.floor(window.innerHeight / 75);

            this.viewPortBottom = 0;

            mainVideoList.style.height = (this.videos.length * 215) + "px";

            mainVideoList.style.paddingTop = (this.viewPortBottom * 215) + "px";
        } else {
            this.viewPortTop = Number.MAX_SAFE_INTEGER;

            this.viewPortBottom = 0;

            mainVideoList.style.height = "auto";

            mainVideoList.style.paddingTop = "0";
        }
    }
});