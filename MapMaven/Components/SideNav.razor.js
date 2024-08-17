window.sideNavResizer = (function () {
    const minSideNavWidth = 240;
    const maxSideNavWidth = 700;

    let sideNavWidth = localStorage.getItem("sideNavWidth") ?? 240;

    setDrawerWidthNoThrottle(sideNavWidth);

    const setDrawerWidth = throttle(setDrawerWidthNoThrottle, 30);

    function setDrawerWidthNoThrottle(width) {
        // Set the side nav width to the current mouse position
        sideNavWidth = width;
        document.documentElement.style.setProperty("--mud-drawer-width-left", `${width}px`);
    }

    function startSideNavResize(event) {
        // Start tracking mouse movement
        document.addEventListener("mousemove", onMouseMove);
        document.addEventListener("mouseup", stopSideNavResize);
    }

    function onMouseMove(event) {
        if (event.clientX >= minSideNavWidth && event.clientX <= maxSideNavWidth)
            setDrawerWidth(event.clientX);
    }

    function stopSideNavResize() {
        document.removeEventListener("mouseup", stopSideNavResize);
        document.removeEventListener("mousemove", onMouseMove);

        localStorage.setItem("sideNavWidth", sideNavWidth);
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

    function initialize() {
        const resizerHandle = document.querySelector(".resizer-handle");
        resizerHandle.addEventListener("mousedown", startSideNavResize);
    }

    return {
        initialize: initialize
    }
})()