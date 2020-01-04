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
                this.uniqueList = null;

                var waiter = fetch("/api/values/list/all/" + number);

                this.inputListModeNumber = number;

                this.channelMode = false;

                this.uniqueListMode = true;

                this.videoListMode = false;

                var result = await waiter;

                this.uniqueList = await result.json();
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
        showTags() {
            this.channelMode = false;

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
        async onSubmit() {
            if (this.addChannelName != "") {
                this.addChannelName = this.addChannelName.trim();

                this.inputMode = false;

                await fetch("/api/values/channel/" + this.addChannelName, { method: "POST" });

                this.addChannelName = "";

                this.update();

                if (this.channelMode) {
                    await this.showChannels();

                    await this.showChannels();
                }

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
            window.open("https://www.youtube.com/watch?v=" + link, "_blank");
        },
        async downloadVideo(video) {
            fetch("/api/values/download/" + video.id, { method: "POST" });

            Vue.set(video, 'dlstatus', 1);

            var updater = setInterval(async function () {
                var result = await fetch("/api/values/status/downloadResult/" + video.id);

                var status = await result.json();

                if (status.kind == "downloadResult") {
                    Vue.set(video, 'dlstatus', ((status.value) ? 2 : 0));

                    clearInterval(updater);
                }
            }, 5000);
        },
        async deleteVideo(video) {
            if (this.uniqueListMode) {
                var waiter = fetch("/api/values/video/" + video.id, { method: "DELETE" });

                var index = this.uniqueList.indexOf(video);

                this.uniqueList.splice(index, 1);

                await waiter;

                this.listUpdate();
            } else {
                var waiter = fetch("/api/values/video/" + video.id, { method: "DELETE" });

                var index = this.videos.indexOf(video);

                this.videos.splice(index, 1);
            }
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

            if (this.tagsMode) {
                this.showTags();
            }

            this.channelMode = !this.channelMode;

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
        lockChannel(ch) {
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

            this.lockChannel(ch);
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
        }
    },
    el: "#app",
    async mounted() {
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