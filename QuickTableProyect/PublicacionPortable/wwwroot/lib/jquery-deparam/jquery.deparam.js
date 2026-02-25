// jquery.deparam.js
(function ($) {
    $.deparam = $.deparam || function (uri) {
        if (uri === undefined) {
            uri = window.location.search;
        }
        var queryString = {};
        uri.replace(/[?&]+([^=&]+)=([^&]*)/gi, function (m, key, value) {
            key = decodeURIComponent(key);
            value = decodeURIComponent(value);
            if (queryString[key]) {
                if (!Array.isArray(queryString[key])) {
                    queryString[key] = [queryString[key]];
                }
                queryString[key].push(value);
            } else {
                queryString[key] = value;
            }
        });
        return queryString;
    };
})(jQuery);