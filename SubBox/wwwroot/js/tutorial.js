var app = new Vue({
    methods: {
        async leave() {
            await fetch("api/values/firstdone", { method: "POST" });

            window.location.replace("http://localhost:5000/");
        }
    },
    el: "#app"
});