window.GetCookie = function (cookieName) {
  cookieName += '=';
  let decodedCookie = decodeURIComponent(document.cookie);
  var cookies = decodedCookie.split(';');
  for (var i = 0; i < cookies.length; i++) {
    var c = cookies[i];
    while (c.charAt(0) == ' ') {
      c = c.substring(1);
    }

    if (c.indexOf(name) == 0) {
      return c.substring(cookieName.length, c.length);
    }
  }
}

window.Alert = function (message) {
  alert(message);
}

window.Confirm = function (message) {
  return confirm(message);
}

window.Blur = function (element) {
  element.blur();
}

window.ScrollUp = function (element) {
  element.scrollTo({
    top: 0
  });
}

window.ScrollDown = function (element) {
  element.scrollTo({
    top: 10000000
  });
}