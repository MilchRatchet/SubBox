var app = new Vue({
    data: {
        videos: [],
        filter: "",
        selectedDir: "",
        selectedThumbDir: "",
        selectedSize: 0,
    },
    computed: {
        filteredVideos: function () {
            if (this.filter === "") {
                return this.videos;
            }

            console.log("ok");

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
        },
        async select(video) {  
            window.scrollTo(0, 0);

            document.body.style.overflow = "hidden";

            this.selectedDir = video.Value.dir; 

            this.selectedThumbDir = video.Value.thumbDir;

            this.selectedSize = Math.round(video.Value.size / (1024 * 1024) * 10) / 10;

            document.getElementsByTagName('video')[0].volume = 0.1;
        },
        async deleteVideo() {
            var re = new RegExp('\\\\', 'g');

            await fetch("/api/values/localvideo/" + this.selectedDir.replace(re,'*') + "/" + this.selectedThumbDir.replace(re,'*'), { method: "DELETE" }); 

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

            for (var i = 0; i < sURLVariables.length; i++) 
            {
                var sParameterName = sURLVariables[i].split('=');

                if (sParameterName[0] == sParam) 
                {
                    return sParameterName[1];
                }
            }

            return null;
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
                window.location.replace("http://localhost:5000");
            }
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