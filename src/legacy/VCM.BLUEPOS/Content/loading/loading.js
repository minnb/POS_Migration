var loading =
{
    createloading: function ()
    {
        
        var loading = "<div id='fountain_loading' class='ui-ios-overlay-shadow'>" +
                           "<div class='ui-ios-overlay ios-overlay-show'>" +
                            "<div class='loader charcoal'><div class='spinner'><div class='outer'></div><div class='inner'></div></div></div>"+
                           "</div>" +
                       "</div>";

        $("body").append(loading);
    },
    showloading: function ()
    {
        loading.createloading();
    },
    hideloading: function ()
    {
        $("#fountain_loading").remove();
    }   
}
