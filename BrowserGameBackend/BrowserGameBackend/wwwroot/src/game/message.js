export class Message{
    constructor(message, ttl = 5000){
        this.message = message;
        this.ttl = ttl
    }
}