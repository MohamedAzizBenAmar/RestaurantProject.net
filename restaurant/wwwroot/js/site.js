$(document).ready(function () {
    // Smooth scroll for anchor links, excluding dropdowns
    $('a.nav-link').on('click', function (event) {
        if (this.hash !== "" && !$(this).is('[data-bs-toggle="dropdown"]')) {
            event.preventDefault();
            var hash = this.hash;
            $('html, body').animate({
                scrollTop: $(hash).offset().top
            }, 800);
        }
    });
});