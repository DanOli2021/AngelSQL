function isPrivateNetwork() {
    const hostname = window.location.hostname;

    // Si es localhost, lo consideramos como red privada
    if (hostname === "localhost" || hostname === "127.0.0.1") {
        return true;
    }

    // Si hostname es una IP, comprobamos si es privada
    const parts = hostname.split(".").map(Number);
    if (parts.length === 4 && parts.every(num => num >= 0 && num <= 255)) {
        // Clase A: 10.0.0.0 - 10.255.255.255
        if (parts[0] === 10) {
            return true;
        }
        // Clase B: 172.16.0.0 - 172.31.255.255
        if (parts[0] === 172 && parts[1] >= 16 && parts[1] <= 31) {
            return true;
        }
        // Clase C: 192.168.0.0 - 192.168.255.255
        if (parts[0] === 192 && parts[1] === 168) {
            return true;
        }
    }

    // Si no cumple con ninguna de las condiciones anteriores, es una IP pública
    return false;
}

