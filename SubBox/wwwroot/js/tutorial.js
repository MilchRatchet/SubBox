var app = new Vue({
    data: {
        slide: 0,
        lastSlideChange: null,
    },
    methods: {
        async leave() {
            await fetch("api/values/firstdone", { method: "POST" });

            window.location.replace("http://localhost:2828/");
        }
    },
    el: "#app",
    mounted() {
        const page = document.getElementById("app");

        page.addEventListener("contextmenu", function (event) {
            event.preventDefault();
        }, false);

        this.lastSlideChange = Date.now();

        window.addEventListener('wheel', function(event) {
            var newTime = Date.now();

            if (newTime - app.lastSlideChange > 300) {
                if (event.deltaY > 0) {
                    app.slide++;
                } else {
                    app.slide--;
                }

                if (app.slide < 0) {
                    app.slide = 0;
                } else if (app.slide > 5) {
                    app.slide = 5;
                }

                app.lastSlideChange = newTime;
            }

        });
    }
});