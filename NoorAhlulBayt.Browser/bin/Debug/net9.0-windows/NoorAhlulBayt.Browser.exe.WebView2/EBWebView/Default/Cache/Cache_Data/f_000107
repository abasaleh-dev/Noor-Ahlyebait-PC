
  var	iiiiiiiiiii=0;
  
  
function URLEncode (clearString) {
  var output = '';
  var x = 0;

  clearString = clearString.toString();
  var regex = /(^[a-zA-Z0-9_.]*)/;
  while (x < clearString.length) {
    var match = regex.exec(clearString.substr(x));
    if (match != null && match.length > 1 && match[1] != '') {
    	output += match[1];
      x += match[1].length;
    } else {
      if (clearString[x] == ' ')
        output += '+';
      else {
        var charCode = clearString.charCodeAt(x);
        var hexVal = charCode.toString(16);
        output += '%' + ( hexVal.length < 2 ? '0' : '' ) + hexVal.toUpperCase();
      }
      x++;
    }
  }
  return output;
}

function URLDecode (encodedString) {
  var output = encodedString;
  var binVal, thisString;
  var myregexp = /(%[^%]{2})/;
  while ((match = myregexp.exec(output)) != null
             && match.length > 1
             && match[1] != '') {
    binVal = parseInt(match[1].substr(1),16);
    thisString = String.fromCharCode(binVal);
    output = output.replace(match[1], thisString);
  }
  return output;
}
(function($){
	function getPageScroll() {
		var xScroll, yScroll;
		if (self.pageYOffset) {
			yScroll = self.pageYOffset;
			xScroll = self.pageXOffset;
		} else if (document.documentElement && document.documentElement.scrollTop) {	 // Explorer 6 Strict
			yScroll = document.documentElement.scrollTop;
			xScroll = document.documentElement.scrollLeft;
		} else if (document.body) {// all other Explorers
			yScroll = document.body.scrollTop;
			xScroll = document.body.scrollLeft;	
		}
		var arrayPageScroll = {'xScroll':xScroll,'yScroll':yScroll};
		return arrayPageScroll;
	}	
	
	function getPageSize() {
		var xScroll, yScroll;

		if (window.innerHeight && window.scrollMaxY) {	
			xScroll = window.innerWidth + window.scrollMaxX;
			yScroll = window.innerHeight + window.scrollMaxY;
		} else if (document.body.scrollHeight > document.body.offsetHeight){ // all but Explorer Mac
			xScroll = document.body.scrollWidth;
			yScroll = document.body.scrollHeight;
		} else { // Explorer Mac...would also work in Explorer 6 Strict, Mozilla and Safari
			xScroll = document.body.offsetWidth;
			yScroll = document.body.offsetHeight;
		}

		var windowWidth, windowHeight;

		if (self.innerHeight) {	// all except Explorer
			if(document.documentElement.clientWidth){
				windowWidth = document.documentElement.clientWidth; 
			} else {
				windowWidth = self.innerWidth;
			}
			windowHeight = self.innerHeight;
		} else if (document.documentElement && document.documentElement.clientHeight) { // Explorer 6 Strict Mode
			windowWidth = document.documentElement.clientWidth;
			windowHeight = document.documentElement.clientHeight;
		} else if (document.body) { // other Explorers
			windowWidth = document.body.clientWidth;
			windowHeight = document.body.clientHeight;
		}	

		// for small pages with total height less then height of the viewport
		if(yScroll < windowHeight){
			pageHeight = windowHeight;
		} else { 
			pageHeight = yScroll;
		}


		// for small pages with total width less then width of the viewport
		if(xScroll < windowWidth){	
			pageWidth = xScroll;		
		} else {
			pageWidth = windowWidth;
		}
		
		//
		var largestWidth;
		var largestHeight;
		var smallestWidth;
		var smallestHeight;
		//
		if ( pageWidth >= windowWidth )
		{	largestWidth = pageWidth; smallestWidth = windowWidth;	}
		else
		{	largestWidth = windowWidth; smallestWidth = pageWidth;	}
		//
		if ( pageHeight >= windowHeight )
		{	largestHeight = pageHeight; smallestHeight = windowHeight;	}
		else
		{	largestHeight = windowHeight; smallestHeight = pageHeight;	}
		
		// Return
		var arrayPageSize = {'pageWidth':pageWidth,'pageHeight':pageHeight,'windowWidth':windowWidth,'windowHeight':windowHeight,'largestWidth':largestWidth,'largestHeight':largestHeight};
		return arrayPageSize;
	}
		
	$().ready(function(){
		
		//menu
		$(".header_nav").each(function(){
			$(">ul>li", this).each(function(){
				
				$(this).bind("mouseover", function(){
					if ($(".submenu", this).length) {
						$(".submenu", this).show();
					}
					$(this).addClass("hover");
				});
				$(this).bind("mouseout", function(){
					if ($(".submenu", this).length) {
						$(".submenu", this).hide();
					}
					$(this).removeClass("hover");
				});
			});
		});
	
		
	
		
		$(".posts").each(function(){
			var animate_loading = function(e){
				if ($(".loading_bar", e).length) {
					$(".loading_bar", e).animate({left:180}, {
						queue:false, duration:600,  complete:function(){
							$(this).css({left:-170});
							window.setTimeout(function(){
								animate_loading(e);
							}, 500);
						}
					});
				}
			};
			
			var animate_scrollTo = function(t) {
				var s = getPageScroll();
				if (s.yScroll < t) {
					window.scrollTo(s.xScroll, s.yScroll + 20);
					window.setTimeout(function(){
						animate_scrollTo(t);
					}, 20);
				}
			};
			
			var hide_opened_post = function(){
				var c = $("li.post_content_opened", posts);
				r = 0;
				if ( c.length ) {
					c.removeClass("selected");
					r = $(".post_content",c).outerHeight();
					
				}
				
				return r;
			};
			
			var show_post_content = function(e, h, f) {
				var r = hide_opened_post();
				e = $(e);
				var cnt = $(".post_content", e);
				if (cnt.length) {
					if ( h == undefined ) {
						hh = parseInt($(e).data("post_content_height")) || undefined;
						if (h == undefined) {
							hh = "show";
						} else {
							hh = h +"px";
						}
					} else {
						hh = h+"px";
					}
					cnt.animate({height:"show"}, {
						queue:false, duration:400,  complete:function(){
							
							var o = e.offset(); o.top -= 20;
							var s = getPageScroll();
							if ( o.top < s.yScroll ) {
								$.scrollTo(o.top, 600);
								//$('html,body').animate({scrollTop: o.top}, {queue:false, duration:900, easing:"easeInOutExpo"});
							} else {
								var z = getPageSize();
								h = parseInt(h) || 0;
								h = o.top - s.yScroll + h - 0;
								if ( h > z.windowHeight ) {
									$.scrollTo(o.top, 600);
									//$('html,body').animate({scrollTop: o.top}, {queue:false, duration:900, easing:"easeInOutExpo"});
								}
							}
							
							$(this).parents("li").addClass("post_content_opened");
							if (typeof f == "function") {
								f();
							}
						}
					});
				}
			};
			
			var posts_waiting = false;
			var posts = this;
			
			$("a.post_ajax_title", this).each(function(){
				var self = this;
				var li = $($(this).parents("li")[0]);
				
				
				$(this).bind("click", function(){
					this.blur();
					if (posts_waiting) return false;
					
					
					li.addClass("selected");
					
					
				  posts_waiting = true;
					var href = $(this).attr("href");
					if (href != "") {
						if (li.is(".post_ajax_loaded")) {
							if (li.is(".post_content_opened")) {
								li.removeClass("selected");
								
								$(".post_content", li).animate({height:"hide"}, {
									queue:false, duration:400, complete:function(){
										posts_waiting = false;
										li.removeClass("post_content_opened");
										
									}
								});
							} else {
								li.addClass("selected");
								
								show_post_content(li, $(li).data("post_content_height"), function(){
									posts_waiting = false;
									
								
								});
								
								
								/*
								$(".post_content", li).animate({height:"show"}, {
									queue:false, duration:800, easing: "easeInOutExpo", complete:function(){
										posts_waiting = false;
										li.addClass("post_content_opened");
									}
								});
								*/
							}
						} else {
							li.append( $("<div></div>").addClass("loading").append( $("<div></div>").addClass("loading_bar") ) );
							animate_loading(li);
							
							//hide_opened_post();
							
							$.ajax({
								url:href,
								data:"",
								type:"GET",
								success: function(msg) {
									li.addClass("post_ajax_loaded");
									$(".loading", li).remove();
									var cnt = $(".post_content", li);
									if (!cnt.length) {
										cnt = $("<div></div").addClass("post_content").append( $("<div></div>").addClass("post_content_wrapper"));
									}
									cnt.hide().find(".post_content_wrapper").html(msg);									
									li.append(cnt);
									cnt.show().css({height:"auto"});
									var h = cnt.outerHeight();
									cnt.hide();
									
									$(li).data("post_content_height", h);
									
									show_post_content(li,h, function(){
										posts_waiting = false;
									});
									
									
								},
								error: function(){
									posts_waiting = false;
									$(".loading", li).remove();
								}
							});
						}
					}
					return false;
				});
			});
		
		
		
		$("a.more-link", this).each(function(){
				var self = this;
				var li = $($(this).parents("li")[0]);
				
				$(".post_content_more").hide();
				
				$(this).bind("click", function(){
					
				$(this).hide();
					
				  posts_waiting = true;
					var href = $(this).attr("href");
					if (href != "") {
						
							if (li.is(".post_content_opened")) {
								
								$(".post_content_more", li).animate({height:"show"}, {
									queue:false, duration:100,  complete:function(){
										posts_waiting = false;
										
									}
								});
							
						} 
					}
					return false;
				});
			});
		
		
		
	
		
		
		
			
		$("a.post_coll", this).each(function(){
				var self = this;
				var li = $($(this).parents("li")[0]);
				
				 
				$(this).bind("click", function(){
					
					if (posts_waiting) return false;
		
				
						if ($($('ul.posts').get(jjjjjjjjjjj)).children().is(".post_ajax_loaded")) {
							if ($($('ul.posts').get(jjjjjjjjjjj)).children().is(".post_content_opened")) {
								$($('ul.posts').get(jjjjjjjjjjj)).children().removeClass("selected");
								$(".post_content", $($('ul.posts').get(jjjjjjjjjjj)).children()).animate({height:"hide"}, {
									 complete:function(){
										posts_waiting = false;
										$($('ul.posts').get(jjjjjjjjjjj)).children().removeClass("post_content_opened");
									}
								});
							}
							}
						
			});
			
			});
			

		$("a.post_exp", this).each(function(){
		
				var self = this;
				
	
				
				$(this).bind("click", function(){
						if ($($('ul.posts').get(iiiiiiiiiii)).children().is(".post_ajax_loaded")) {
						$($('ul.posts').get(iiiiiiiiiii)).children().addClass("selected");
								$(".post_content", $($('ul.posts').get(iiiiiiiiiii)).children()).animate({height:"show"}, {
									duration:300, complete:function(){
										posts_waiting = false;
										$($('ul.posts').get(iiiiiiiiiii)).children().addClass("post_content_opened");
									}
								});
							}
						
			});
		
			});
	//		
			
			

			
			if ( $(".post_content", posts).length) {
				var def_li = $("li", posts).filter(".selected")[0];
				$(".post_content", posts).each(function(){
					var li = $(this).parents("li");
					$(this).show().css({height:"auto"});
					li.data("post_content_height", $(this).outerHeight());
					li.addClass("post_ajax_loaded").removeClass("post_content_opened").removeClass("selected");
					$(this).hide();
				});
				
				if (def_li) {
					//$(def_li).addClass("selected").addClass("post_content_opened").find(".post_content").show();
					window.setTimeout(function(){
						var h = $(def_li).data("post_content_height");
						$(def_li).addClass("selected");
						show_post_content(def_li, h);
					},500);
				}
			}
		});
		
		
		//if ($("script[src^='http://w.sharethis.com/button']").length) {
			/*
			console.debug(_stFpc());
			console.debug(SHARETHIS.sessionID);
			console.debug(SHARETHIS.fpc);
			console.debug(encodeURIComponent(document.location.href));
			
			$.ajax({
				url:"http://w.sharethis.com/api/createSharURL_ws.php",
				data:"url=" + encodeURIComponent(document.location.href) + "&sessionID=" + SHARETHIS.sessionID + "&fpc=" + SHARETHIS.fpc,
				type: "POST",
				dataType: "json",
				success: function(msg) {
					console.debug(msg);
				}
			});
			*/
			
			
			$(".button_share").each(function(){
				var title=$("title").html().replace(/^\s+|\s+$/g,"");
				var iframe_width = 370;
				var iframe_height = 200;
				var elem = this;
				var elem_off = $(elem).offset();
				var elem_width = $(elem).innerWidth();
				var elem_height = $(elem).innerHeight();
				
				$(elem).parent().addClass("button_share_container");
				
				var showShareBox = function() {
					this.blur();
					$("#sharebox").show();
					return false;
				};
				
				if (!$("#sharebox").length) {
				
					$.ajax({
						url: "http://" + window.location.hostname + "/share.php?title=" + URLEncode(title) +"&url=" + URLEncode(document.URL),
						//url: "http://webexcellence.som/customer/helloewy/share.php?title=" + URLEncode(title) +"&url=" + URLEncode(document.URL),
						type: "get",
						success: function(xhtml) {
							$("<div />").attr("id", "sharebox")
								.css({
									position:"absolute",
									left:"0px",
									top:"100%",
									zIndex:10000,
									width:iframe_width+"px",
									overflow:"hidden"
								})
								.html(xhtml)
								.appendTo( $(elem).parent() )
								.hide();
								
							initShareBox();
						}
					});
					
					
					$(elem).bind("click", showShareBox);
				}
				
				$("#sharebox").hide();
			});
			
		//}
		
		//tags button - hide
		$("*").bind("click", function(e){
			if (!e.target) return;
			/*
			if (!$(e.target).parents(".post_menu_tags").length) {
				$(".post_menu_tags").removeClass("selected");
			}
			*/
			if (!$(e.target).is(".button_tags")) {
				$(".button_tags").parent().removeClass("selected");
				//$(".post_menu_tags").removeClass("selected");
			}
			
			if (!$(e.target).is(".button_share")) {
				if (!$(e.target).parents(".button_share_container").length) {
					$("#sharebox").hide();
				}
			}
		});
	});
})(jQuery);

//Alternating Row function. Used on All Categories page
function alternate(id){
	if(document.getElementsByTagName){  
    var table = document.getElementById(id);  
    var rows = table.getElementsByTagName("li");  
    for(i = 0; i < rows.length; i++){          
   //manipulate rows
     if(i % 2 == 0){
       rows[i].className = "";
     }else{
       rows[i].className = "odd";
     }      
   }
 }
}
function post_like(m,n,k)
	{   var theme_id=k;
        var question_id = m;
	    var faq_id=n;
        var datas = "post_like="+question_id+"&"+"post_show_hits="+faq_id+"&"+"pltheme_id="+theme_id;
		jQuery.ajax({  
        type: "POST",  
        url: ajax_url + "=wp_post_like",		
        data: datas,  
        success: function(html){  
            jQuery("#post_like_hits_div"+question_id).html(html);
			
        }  
    });
	}
function post_unlike(m,n,k)
	{   var theme_id=k;
        var question_id = m;
	    var faq_id=n;
        var datas = "post_like="+question_id+"&"+"post_show_hits="+faq_id+"&"+"punltheme_id="+theme_id;
		
		jQuery.ajax({  
        type: "POST",  
        url: ajax_url + "=wp_post_unlike",  
        data: datas,  
        success: function(html){  
            jQuery("#post_like_hits_div"+question_id).html(html);
			
        }  
    });
	}
function like(m,n,k)
    {   
     	var theme_id=k;
        var question_id = m;
	    var faq_id=n;
        var datas = "like="+question_id+"&"+"show_hits="+faq_id+"&"+"ltheme_id="+theme_id;
		
		jQuery.ajax({  
        type: "POST",  
        url: ajax_url + "=wp_like",  
        data: datas,  
        success: function(html){  
            jQuery("#like_hits_div"+question_id).html(html);
			
        }  
    });
	
		  
	}

function unlike(m,n,k)	

		{ var theme_id=k;
          var question_id = m;
		  var faq_id=n;
          var datas = "unlike="+question_id+"&"+"show_hits="+faq_id+"&"+"unltheme_id="+theme_id;
		
		jQuery.ajax({  
        type: "POST",  
		url: ajax_url + "=wp_unlike",        
        data: datas,  
        success: function(html){  
           jQuery("#like_hits_div"+question_id).html(html);			
        }  
    });

		}
function hits(m,n,k)	

		{
		var theme_id=k;
		var question_id = m;
		var faq_id=n;
		if(jQuery("#post_content"+question_id).css( "display" )=="none")
        {
		var datas = "hits="+question_id+"&"+"faq_hit_id="+faq_id;
		jQuery.ajax({  
        type: "POST",  
        url: ajax_url + "=wp_hits",		
        data: datas,  
        success: function(html){  
            jQuery("#hits"+question_id).html(html);
			
		
        }  
    });
        }
		}	
	
  function post_hits(m,n,k)
       {
		var question_id = m;
		 var post_faq_id=n;
		if(jQuery("#post_content"+question_id).css( "display" )=="none")
        {
		var datas = "post_hits="+question_id+"&"+"post_faq_id="+post_faq_id;
		jQuery.ajax({  
        type: "POST",  
        url: ajax_url + "=wp_post_hits",		
        data: datas,  
        success: function(html){  
            jQuery("#post_hits"+question_id).html(html);
		
        }  
    });
        }
		}
		  
		
  function expand_hits(tiv,m,k,q,w)
  {
     var faq_id=m;
     var datas="faq_id="+faq_id+"&"+"tiv="+tiv;
	 jQuery.ajax({  
        type: "POST",  
        url: ajax_url + "=wp_expand_hits",				
        data: datas,  
        success: function(html){
        var tox=html;		
        var myarr = tox.split(".");
        var hits="Hits: ";		
   for(var i=0;i<=(myarr.length)-2;i++)
       {  
	     var k=myarr[i];
		  var hit=k.split(",");
		   var ch=hits+hit[1];
		    edit_title(tiv,hit[0],faq_id,q,w);
		            jQuery("#hits"+hit[0]).html(ch);
	   }	
      }   
    }); 
  }
   
  function expand_post_hits(tiv,m,k,q,w)
   {
     var faq_id=m;
      var datas="faq_post_id="+faq_id+"&"+"post_tiv="+tiv;
		jQuery.ajax({  
        type: "POST",  
        url: ajax_url + "=wp_expand_post_hits",		
        data: datas,  
        success: function(html){
        var tox=html;		
        var myarr = tox.split(".");
        var hits="Hits: ";		
      for(var i=0;i<=(myarr.length)-2;i++)
        {
          var k=myarr[i];
		    var hit=k.split(",");
		      var ch=hits+hit[1];
			    edit_title(tiv,hit[0],faq_id,q,w);
		       jQuery("#post_hits"+hit[0]).html(ch);

		}		
       }  
    });   
   }
   
   function edit_title(q,m,n,j,i){
    var k="#"+i;
	var l="#"+j;
	if(q==1)
	{
	if(jQuery("#post_content"+m).css( "display" )=="none")
	{
	jQuery('#post_span'+m+n).find("h2").css("background-color",k);
	jQuery('#post_span'+m+n).find("h2").css("border-color",l);
	}
	else
	{
	jQuery('#post_span'+m+n).find("h2").css({"border-color":k,"background-color":l});
	}
	}
	else
	{
	jQuery('#post_span'+m+n).find("h2").css({"border-color":k,"background-color":l});
	}
	} 