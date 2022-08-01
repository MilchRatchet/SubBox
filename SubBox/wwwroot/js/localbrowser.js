var app = new Vue({
    data: {
        videos: [],
        filter: "",
        selectedDir: "",
        selectedThumbDir: "",
        selectedSize: 0,
        deletionMode: false,
    },
    computed: {
        filteredVideos: function () {
            if (this.filter === "") {
                return this.videos;
            }

            filterUp = this.filter.toUpperCase();

            return this.videos.filter(function (u) {
                return (u.Value.data.title + u.Value.data.description + u.Value.data.channelTitle).toUpperCase().includes(filterUp);
            });
        },
    },
    methods: {
        async update() {
            var result = await fetch("/api/values/localvideos");

            this.videos = await result.json();

            this.videos.forEach(function (item) {
                item.Value.onlineThumbUrl = "https://i.ytimg.com/vi/" + item.Key + "/mqdefault.jpg"
            });
        },
        async select(video) {
            if (this.deletionMode) {
                var re = new RegExp('\\\\', 'g');

                fetch("/api/values/localvideo/" + video.Value.dir.replace(re, '*') + "/" + video.Value.thumbDir.replace(re, '*'), { method: "DELETE" });

                this.videos.splice(this.videos.indexOf(video), 1);

                return;
            }

            window.scrollTo(0, 0);

            document.body.style.overflow = "hidden";

            this.selectedDir = video.Value.dir;

            this.selectedThumbDir = video.Value.thumbDir;

            this.selectedSize = Math.round(video.Value.size / (1024 * 1024) * 10) / 10;

            document.getElementsByTagName('video')[0].volume = 0.1;
        },
        async deleteVideo() {
            var re = new RegExp('\\\\', 'g');

            await fetch("/api/values/localvideo/" + this.selectedDir.replace(re, '*') + "/" + this.selectedThumbDir.replace(re, '*'), { method: "DELETE" });

            this.back();

            this.update();
        },
        back() {
            this.selectedDir = "";

            document.body.style.overflow = "auto";
        },
        GetURLParameter(sParam) {
            var sPageURL = window.location.search.substring(1);

            var sURLVariables = sPageURL.split('&');

            for (var i = 0; i < sURLVariables.length; i++) {
                var sParameterName = sURLVariables[i].split('=');

                if (sParameterName[0] == sParam) {
                    return sParameterName[1];
                }
            }

            return null;
        },
        switchToOnlineThumb(video, event) {
            video.Value.thumbDir = video.Value.onlineThumbUrl;

            event = event || window.event;

            const target = event.target || event.srcElement;

            target.src = video.Value.thumbDir;
        },
    },
    el: "#app",
    async mounted() {
        const page = document.getElementById("app");

        page.addEventListener("contextmenu", function (event) {
            event.preventDefault();
        }, false);

        document.addEventListener("keyup", function (event) {
            if (event.code === "Escape") {
                window.location.replace("http://localhost:2828");
            }
        }, false);

        document.addEventListener("keydown", function (event) {
            if (document.activeElement.nodeName === "INPUT") return;

            document.getElementById("search").focus();
        }, false);

        await this.update();

        var openId = this.GetURLParameter('id');

        if (openId !== null) {
            var video = this.videos.find((v) => v.Value.data.id == openId);

            if (video !== undefined) {
                this.select(video);
            }
        }
    }
});