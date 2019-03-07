// necessário JQuery
// função que remove um inline-style de um elemento
// credits: https://stackoverflow.com/questions/19917026/bootstrap-modal-z-index

(function ($) {
    $.fn.removeStyle = function (style) {
        var search = new RegExp(style + '[^;]+;?', 'g');

        return this.each(function () {
            $(this).attr('style', function (i, style) {
                return style && style.replace(search, '');
            });
        });
    };
}(jQuery));
