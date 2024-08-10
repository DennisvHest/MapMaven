const sideNavResizer = (function () {
    const resizerHandle = document.querySelector(".resizer-handle");

    const maxSideNavWidth = 700;

    const setDrawerWidth = throttle((width) => {
        // Set the side nav width to the current mouse position
        document.documentElement.style.setProperty("--mud-drawer-width-left", `${width}px`);
    }, 30);

    function startSideNavResize(event) {
        // Start tracking mouse movement
        document.addEventListener("mousemove", onMouseMove);
        document.addEventListener("mouseup", stopSideNavResize);
    }

    function onMouseMove(event) {
        if (event.clientX < maxSideNavWidth)
            setDrawerWidth(event.clientX);
    }

    function stopSideNavResize() {
        document.removeEventListener("mouseup", stopSideNavResize);
        document.removeEventListener("mousemove", onMouseMove);
    }

    function throttle(mainFunction, delay) {
        let timerFlag = null;

        return (...args) => {
            if (timerFlag === null)
                mainFunction(...args);
            timerFlag = setTimeout(() => {
                timerFlag = null;
            }, delay);
        }
    };

    resizerHandle.addEventListener("mousedown", startSideNavResize);
})()