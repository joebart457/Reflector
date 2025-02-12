class TokenizerRule {
    type: string;
    stringToMatch: string;
    replaceWith: string;
    enclosingLeft: string;
    enclosingRight: string;
    ignoreCase: boolean;

    constructor(type: string, stringToMatch: string, replaceWith: string | null = null, enclosingLeft: string = "", enclosingRight: string = "", ignoreCase: boolean = false) {
        this.type = type;
        this.stringToMatch = stringToMatch;
        this.replaceWith = replaceWith ?? stringToMatch;
        this.enclosingLeft = enclosingLeft;
        this.enclosingRight = enclosingRight;
        this.ignoreCase = ignoreCase;
        if (this.enclosingLeft && !this.enclosingRight) {
            this.enclosingRight = this.enclosingLeft;
        }
    }

    get length(): number {
        return this.stringToMatch.length;
    }

    get isEnclosed(): boolean {
        return !!this.enclosingLeft && !!this.enclosingRight;
    }
}