function Binary(object) {
    if (!object) return "";
    return object.toString().split('').map((char) => char.charCodeAt(0).toString(2)).join(' ');
}