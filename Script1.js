// JavaScript source code
var path = window.location.pathname;
const myArray = path.split("/");
let st = "";
for (var i = 0; i < myArray.length; i++) {
	st += myArray[i];

}
var lastKnownScrollPosition = window.scrollY;
window.localStorage.setItem(st, lastKnownScrollPosition);

