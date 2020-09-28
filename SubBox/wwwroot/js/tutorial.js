var app = new Vue({
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
    }
});