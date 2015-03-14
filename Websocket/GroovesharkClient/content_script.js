var songleft;
var allSongsId = [];
var lastPlayedSongs = [];
var actionTable = {};
var lastPlay;
var forcePlay = false;
var playingRandom = false;
var followingList = [];

// GroovesharkUtils
var GU = {
 'skip': function()
    {
      
    },
	'startBroadcasting': function(bc)
    {
        var properties = { 'Description': bc.Description, 'Name': bc.Name, 'Tag': bc.Tag };
        if (GS.getCurrentBroadcast() === false) {
            console.log("Creating broadcast");
            GS.Services.SWF.startBroadcast(properties);
            setTimeout(GU.startBroadcasting, 3000, bc);
            return;
        }
        else if (GS.isBroadcaster() === false)
        {
            console.log("Taking over broadcast");
            GS.Services.takeOverBroadcast(bc.BroadcastID);
            GS.Services.SWF.startBroadcast(properties);
            setTimeout(GU.startBroadcasting, 3000, bc);
            return;
        }
        GU.renameBroadcast();
        setTimeout(function() {
            GU.sendMsg(GUParams.welcomeMessage);
        }, 1000);
        // Remove all the messages in chat
        GU.removeMsg();
        GU.openSidePanel();
        GS.Services.API.userGetSongIDsInLibrary().then(function (result){
            allSongsId = result.SongIDs;
        });
        if ($('#lightbox-close').length == 1)
        {
            $('#lightbox-close').click();
        }
        lastPlay = new Date();
        // Check if we are not playing any song.
        setInterval(GU.forcePlay, 3000);

        // Overload handlechat
        var handleBroadcastSaved = GS.Services.SWF.handleBroadcastChat;
        GS.Services.SWF.handleBroadcastChat = function(e, t){handleBroadcastSaved(e,t);GU.doParseMessage(t);};
        var handleQueueChange = GS.Services.SWF.queueChange;
        GS.Services.SWF.queueChange = function(e){handleQueueChange(e);GU.queueChange();};
    },
	 'broadcast': function()
    {
        GUParams.userReq = '';
        GUParams.passReq = '';
        if (GS.getLoggedInUserID() <= 0)
            alert('Cannot login!');
        else
        {
            GU.updateFollowing();
            GS.Services.API.getUserLastBroadcast().then(function(bc) {
                GS.Services.SWF.ready.then(function()
                {
                    GS.Services.SWF.joinBroadcast(bc.BroadcastID);
                    setTimeout(GU.startBroadcasting, 4000, bc);
                });
            });
        }
    }
};

actionTable = {
   
    'skip':                 [[GU.inBroadcast, GU.guestCheck],           GU.skip,                 '- Skip the current song.']
};

(function()
{
    var callback_start = function()
    {
		GU.skip();
    }
    var init_check = function ()
    {
        GU.skip();
    }
    init_check();
})()
