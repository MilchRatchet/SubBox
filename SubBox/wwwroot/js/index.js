var app = new Vue({
    data: {
        videos: [],
        oldVideos: [],
        channels: [],
        lists: [],
        uniqueList: [],
        tags: [],
        videoListMode: true,
        inputMode: false,
        trashbinMode: false,
        inputListMode: false,
        tagsMode: false,
        inputListModeNumber: 0,
        addPlayList: "",
        addTag: "",
        addTagFilter: "",
        channelMode: false,
        removeChannel: null,
        uniqueListMode: false,
        addChannelName: "",
        night: false,
        filter: "SubBox",
        searchFilter: "",
        playlistFilter: "",
        selectedTag: null,
        settingsMode: false,
        settingNCT: 1,
        settingRT: 7,
        settingDT: 7,
        settingPPS: 50,
        settingColor: "ff0000",
        settingPQ: 4,
        settings: [],
        changes: false,
        inputContext: false,
        selectedInputForContext: null,
        informationMode: false,
        informationContent: [],
        messages: [],
        isFirefox: false,
        filteredLength: "0:00",
        totalLength: "0:00",
        newestVersion: "1.5.1",
        messageRunningId: 0,
        confirmationResult: false,
        confirmationDone: false,
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

            this.checkDownloadStatus();
        },
        async checkDownloadStatus() {
            var result = await fetch("/api/values/localvideos");

            locals = await result.json();

            this.videos.forEach(function (v) {
                Vue.set(v, 'dlstatus', (locals.some((l) => l.Key == v.id)) ? 2 : 0);
            });
        },
        async listUpdate() {
            var result = await fetch("/api/values/lists");

            this.lists = await result.json();
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

            this.settingRT = this.settings[0];

            this.settingNCT = this.settings[1];

            this.settingDT = this.settings[2];

            this.settingPPS = this.settings[3];

            this.settingColor = this.settings[4];

            document.documentElement.style.setProperty('--mainColor', "#" + this.settingColor);

            document.getElementById("colorPicker").jscolor.fromString(this.settingColor);

            this.setSecondColor();

            this.night = (this.settings[5] == "True");

            this.settingPQ = this.settings[6];

            this.setBodyColor();
        },
        showChannelInput() {
            this.inputListMode = false;

            this.inputMode = !this.inputMode;

            this.addChannelName = "";
        },
        showListInput() {
            this.inputMode = false;

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
        async showOldVideos() {
            var waiter = fetch("api/values/videos/old");

            this.channelMode = false;

            this.trashbinMode = true;

            this.videoListMode = false;

            var result = await waiter;

            this.oldVideos = await result.json();
        },
        showSettings() {
            this.changes = false;

            this.getSettings();

            this.settingsMode = !this.settingsMode;
        },
        async showTags() {
            if (this.tagsMode && !this.channelMode) {
                var waiter = fetch("/api/values/channels");

                var result = await waiter;

                this.channels = await result.json();
            }

            if (!this.tagsMode) {
                this.channelMode = false;
            }   

            if (this.tagsMode) {
                this.tags.forEach(function (t) {
                    t.inEdit = false;
                });
            }

            this.tagsMode = !this.tagsMode;

            this.removeChannel = null;
        },
        async refresh() {
            await fetch("/api/values/refresh", { method: "POST", });

            this.update();
        },
        setColor() {
            this.settingColor = document.getElementById('colorPicker').value;

            document.documentElement.style.setProperty('--mainColor', "#" + this.settingColor);

            this.setSecondColor();

            this.changes = true;
        },
        resetColor() {
            this.settingColor = "DB4437";

            document.getElementById("colorPicker").jscolor.fromString(this.settingColor);

            document.documentElement.style.setProperty('--mainColor', "#" + this.settingColor);

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

                this.inputMode = false;

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

                        if (!status.value) {
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

                        if (channel !== undefined) {
                            const messageId = app.messageRunningId++;

                            app.messages.push({
                                "id": messageId,
                                "title": channel.displayname + " was added",
                                "subtitle": "Username: " + channel.username,
                                "thumbUrl": channel.thumbnailUrl,
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

                if (!Number.isInteger(this.inputListModeNumber)) {
                    console.log(this.inputListModeNumber);
                }

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

                    bar.style.setProperty("background", "linear-gradient(to left, #777777 0 " + (100 - progress.value) + "%, #8f54f1 " + (100 - progress.value) + "%)");
                }
            }, 100);

            var updater = setInterval(async function () {
                var result = await fetch("/api/values/status/downloadResult/" + video.id);

                var status = await result.json();

                if (status.kind === "downloadResult") {

                    status.value = status.value === 'true';

                    Vue.set(video, 'dlstatus', ((status.value) ? 2 : 0));

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

                var index = this.videos.indexOf(video);

                this.videos.splice(index, 1);
            }
        },
        async deleteFilteredVideos() {
            if (!(await this.getConfirmation())) return;

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
        },
        async deleteFilteredPlaylistVideos() {   
            if (!(await this.getConfirmation())) return;

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

            this.playlistFilter = "";
        },
        async deleteNotFilteredPlaylistVideos() {
            if (!(await this.getConfirmation())) return;

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

            this.playlistFilter = "";
        },
        async deleteCompletePlaylist() {
            if (!(await this.getConfirmation())) return;

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
        },
        async reactivateVideo(video) {
            var waiter = fetch("/api/values/video/" + video.id, { method: "POST" });

            var index = this.oldVideos.indexOf(video);

            this.oldVideos.splice(index, 1);

            await waiter;

            this.update();
        },
        openChannel(link) {
            window.open("https://www.youtube.com/user/" + link, "_blank");
        },
        async showChannels() {
            this.removeChannel = null;

            var waiter = fetch("/api/values/channels");

            this.channelMode = !this.channelMode;

            if (this.tagsMode) {
                await this.showTags();
            }

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
        unlockAllChannels() {
            this.channels.forEach(function (c) {
                c.locked = false;
            });
        },
        lockChannel(id) {
            const ch = this.channels.find(c => c.id === id);

            console.log(ch);

            if (ch === undefined) return;

            if (ch.locked) {
                ch.locked = false;
                return;
            }

            this.unlockAllChannels();

            ch.locked = true;
        },
        closeChannels() {
            this.channelMode = false;

            this.removeChannel = null;
        },
        closeUniqueList() {
            this.inputListModeNumber = 0;

            this.videoListMode = true;

            this.uniqueListMode = false;

            this.playlistFilter = "";
        },
        closeTrashbin() {
            this.videoListMode = true;

            this.trashbinMode = false;
        },
        async closeSettings() {
            this.settingRT = document.getElementById('RT').value;

            this.settingNCT = document.getElementById('NCT').value;

            this.settingDT = document.getElementById('DT').value;

            this.settingPPS = document.getElementById('PPS').value;
            
            this.settingsMode = false;

            this.changes = false;

            await Promise.all([fetch("/api/values/settings/RT/" + this.settingRT, { method: "POST" }),

            fetch("/api/values/settings/NCT/" + this.settingNCT, { method: "POST" }),

            fetch("/api/values/settings/DT/" + this.settingDT, { method: "POST" }),

            fetch("/api/values/settings/PPS/" + this.settingPPS, { method: "POST" }),

            fetch("/api/values/settings/COLOR/" + this.settingColor, { method: "POST" }),

            fetch("/api/values/settings/PQ/" + this.settingPQ, { method: "POST" })]);

            fetch("/api/values/settings/save", { method: "POST" });
        },
        async switchDesign() {
            this.night = !this.night;

            this.setBodyColor();

            await fetch("/api/values/settings/NIGHT/" + null, { method: "POST" });

            fetch("/api/values/settings/save", { method: "POST" });
        },
        setBodyColor() {
            if (this.night) {
                document.body.setAttribute('bgcolor', '#131313');
            } else {
                document.body.setAttribute('bgcolor', '#fafafa');
            }
        },
        markChannelDeletion(channel) {
            if (this.removeChannel == null) {
                this.removeChannel = channel;
                return;
            }

            if (this.removeChannel.id == channel.id) {
                this.removeChannel = null;
                return;
            }

            this.removeChannel = channel;
        },
        async deleteChannel(channel) {
            this.removeChannel = null;

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
        changeSetting(setting, number) {
            this.changes = true;

            switch (setting) {
                case "NCT": 
                    this.settingNCT *= 1;

                    this.settingNCT += number;

                    if (this.settingNCT > 90) {
                        this.settingNCT = 90;
                    }

                    else if (this.settingNCT < 1) {
                        this.settingNCT = 1;
                    }
                    
                    break;
                case "RT":
                    this.settingRT *= 1;

                    this.settingRT += number;

                    if (this.settingRT > 7) {
                        this.settingRT = 7;
                    }
                    else if (this.settingRT < 1) {
                        this.settingRT = 1;
                    }

                    break;
                case "DT": 
                    this.settingDT *= 1;

                    this.settingDT += number;

                    if (this.settingDT < 7) {
                        this.settingDT = 7;
                    }

                    break;
                case "PPS":
                    this.settingPPS *= 1;

                    this.settingPPS += number;

                    if (this.settingPPS < 1) {
                        this.settingPPS = 1;
                    }

                    break;
                default:
                    console.log("Unknown setting was tried to change!");

                    console.log(setting);

                    this.changes = false;
            }
        },
        setSecondColor() {
            var r = parseInt(this.settingColor.substr(0, 2), 16);
            var g = parseInt(this.settingColor.substr(2, 2), 16);
            var b = parseInt(this.settingColor.substr(4, 2), 16);
            var yiq = ((r * 299) + (g * 587) + (b * 114)) / 1000;
            if (yiq >= 200) {
                document.documentElement.style.setProperty('--secColor', 'black');
            } else {
                document.documentElement.style.setProperty('--secColor', 'white');
            }
        },
        setFilter(ch) {
            if (ch.displayname == this.filter) {
                this.filter = "SubBox";
            } else {
                this.filter = ch.displayname;
            }

            this.lockChannel(ch.id);
        },
        selectTag(tag) {
            if (this.selectedTag == null) {
                this.selectedTag = tag;

                return;
            }
            if (this.selectedTag.name == tag.name) {
                this.selectedTag = null;

                return;
            }
            this.selectedTag = tag;
        },
        deleteTag(tag) {
            if (this.selectedTag != null && this.selectedTag.name == tag.name) {
                this.selectedTag = null;
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
        resetAllFilters() {
            this.filter = "SubBox";

            this.selectedTag = null;

            this.searchFilter = "";

            this.unlockAllChannels();
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
        async getConfirmation() {
            var menu = document.getElementById('confirmationMenu');

            menu.style.display = "flex";

            const promise = new Promise((resolve, reject) => {

                const interval = setInterval(function () {
                    if (!app.confirmationDone) return;

                    menu.style.display = "";

                    app.confirmationDone = false;

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

            if (this.newestVersion !== this.informationContent[0]) {
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
                    "event": "const index = app.messages.findIndex(m => m.id === '" + messageId + "'); app.messages.splice(index, 1); if (!app.night) app.switchDesign();"
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
                    "event": "const index = app.messages.findIndex(m => m.id === '" + messageId + "'); app.messages.splice(index, 1);"
                });
            }

            if (day === 31 && month === 11) {
                const messageId = this.messageRunningId++;

                this.messages.push({
                    "id": messageId,
                    "title": "Happy New Year",
                    "subtitle": "Looking forward to " + (date.getFullYear() + 1),
                    "thumbUrl": "media/3112.png",
                    "text": '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80' + '\uD83D\uDE80',
                    "event": "const index = app.messages.findIndex(m => m.id === '" + messageId + "'); app.messages.splice(index, 1);"
                });
            }

            if (day === 9 && month === 9) {
                const messageId = this.messageRunningId++;

                this.messages.push({
                    "id": messageId,
                    "title": "Happy Leif Erikson Day",
                    "subtitle": "hinga-dinga-durgen",
                    "thumbUrl": "media/0910.png",
                    "text": "My favorite holiday, next to April Fool's Day",
                    "event": "const index = app.messages.findIndex(m => m.id === '" + messageId + "'); app.messages.splice(index, 1);"
                });
            }

            if (day === 5 && month === 9) {
                const messageId = this.messageRunningId++;

                this.messages.push({
                    "id": messageId,
                    "title": "SubBox is celebrating its Anniversary",
                    "subtitle": "Thanks for using SubBox",
                    "thumbUrl": "media/LogoRed.png",
                    "text": '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89' + '\uD83C\uDF89',
                    "event": "const index = app.messages.findIndex(m => m.id === '" + messageId + "'); app.messages.splice(index, 1);"
                });
            }

            if (day === 1 && month === 3) {
                const messageId = this.messageRunningId++;

                this.messages.push({
                    "id": messageId,
                    "title": "It is April Fool's Day",
                    "subtitle": "Don't trust everything you read today",
                    "thumbUrl": "media/0104.png",
                    "text": "You never know who wants to fool you" + '\uD83D\uDC40',
                    "event": "const index = app.messages.findIndex(m => m.id === '" + messageId + "'); app.messages.splice(index, 1); app.openVideo('dQw4w9WgXcQ');"
                });
            }

            if (day === 14 && month === 1) {
                const messageId = this.messageRunningId++;

                this.messages.push({
                    "id": messageId,
                    "title": "It is Valentins Day",
                    "subtitle": "Did you know that it is Youtube's Birthday?",
                    "thumbUrl": "media/1402.png",
                    "text": "Youtube just turned " + (date.getFullYear() - 2005) + " years",
                    "event": "const index = app.messages.findIndex(m => m.id === '" + messageId + "'); app.messages.splice(index, 1);"
                });
            }
        }
    },
    el: "#app",
    async mounted() {

        this.isFirefox = typeof InstallTrigger !== 'undefined';

        this.getNewestVersion();

        this.specialDayMessages();

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

        var setOv = document.querySelector('.settingsOverlay');

        var chOv = document.querySelector('#channelUI');

        var tagsOv = document.querySelector('#tagsUI');

        var infOv = document.querySelector('#informationOverlay');

        var ctxOv = document.querySelector('#ctxMenu');

        page.addEventListener("click", function (event) {

            event = event || window.event;

            const target = event.target || event.srcElement;

            if (app.settingsMode && target.nodeName !== "BUTTON" && !app.changes) {
                if (!setOv.contains(target)) {
                    app.showSettings();
                }
            }

            if (app.channelMode && target.nodeName !== "BUTTON") {
                if (!chOv.contains(target)) {
                    app.showChannels();
                }
            }

            if (app.tagsMode && target.nodeName !== "BUTTON") {
                if (!tagsOv.contains(target)) {
                    app.showTags();
                }
            }

            if (app.informationMode && !ctxOv.contains(target)) {
                if (!infOv.contains(target)) {
                    app.toggleInformationMode();
                }
            }

            app.inputContext = false;

            var ctxMenu = document.getElementById("ctxMenu");

            ctxMenu.style.display = "";

            ctxMenu.style.left = "";

            ctxMenu.style.top = "";
        }, false);

        document.addEventListener("keyup", function (event) {
            if (event.code === "Escape") {
                if (app.uniqueListMode) {
                    app.closeUniqueList();
                }
                if (app.trashbinMode) {
                    app.closeTrashbin();
                }
            }
        }, false);

        document.body.onmousedown = function (e) { if (e.button === 1) return false; }

        var result = await fetch("/api/values/first");

        var first = await result.json();

        if (first) {
            window.location.replace("http://localhost:5000/tutorial.html");
        }

        this.listUpdate();

        this.update();

        this.getSettings();

        this.tagsUpdate();
    }
});