// The navigation shell is statically rendered; close its native checkbox after selecting a route.
document.addEventListener("click", (event) => {
    if (
        event.target instanceof Element &&
        event.target.closest(".sidebar nav a")
    ) {
        const toggle = document.getElementById("sidebar-toggle");
        if (toggle instanceof HTMLInputElement) toggle.checked = false;
    }
});
