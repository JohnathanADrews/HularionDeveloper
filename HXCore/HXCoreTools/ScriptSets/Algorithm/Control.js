/*
MIT License

Copyright (c) 2023 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/


function Control() {

};
Control.prototype = {

    getObjectMemberNames: function (object) {
        return Object.getOwnPropertyNames(object);
    },

    getObjectMemberValues: function (object) {
        var names = Object.getOwnPropertyNames(object);
        var result = [];
        for (var i = 0; i < names.length; i++) {
            result.push(object[names[i]]);
        }
        return result;
    },

    getObjectMemberNameValuePairs: function (object) {
        if (object == null) { return []; }
        var names = Object.getOwnPropertyNames(object);
        var result = [];
        for (var i = 0; i < names.length; i++) {
            result.push({ name: names[i], value: object[names[i]] });
        }
        return result;
    },

    processObjectMemberValues: function (object, processor) {
        this.processObjectMemberNameValuePairs(object, pair => processor(pair.value));
    },

    processObjectMemberNameValuePairs: function (object, processor) {
        var pairs = this.getObjectMemberNameValuePairs(object);
        this.processArray(pairs, pair => processor(pair));
    },

    clearMembers: function (object) {
        this.processArray(this.getObjectMemberNames(object), name => { delete object[name]; });
    },

    projectOnto: function (source, destination, deleteExcess) {
        if (deleteExcess) { this.clearMembers(destination); }
        var pairs = this.getObjectMemberNameValuePairs(source);
        this.processArray(pairs, pair => { destination[pair.name] = pair.value; });
    },

    processArray: function (array, processor) {
        if (array == null) { return; }
        for (var i = 0; i < array.length; i++) {
            if (processor(array[i], i) === false) { return false; }
        }
        return true;
    },

    processReverseArray: function (array, processor) {
        if (array == null) { return; }
        for (var i = array.length - 1; i >= 0; i--) {
            if (processor(array[i], i) === false) { return false; }
        }
        return true;
    },

    processIterator: function (min, max, processor) {
        for (var i = min; i <= max; i++) {
            if (processor(i) === false) { return false; }
        }
        return true;
    },

    processReverseIterator: function (max, min, processor) {
        for (var i = max; i >= min; i--) {
            if (processor(i) === false) { return false; }
        }
        return true;
    },

    iterate: function (boundary1, boundary2, processor) {
        if (boundary1 <= boundary2) { return this.processIterator(boundary1, boundary2, processor); }
        else { return this.processReverseIterator(boundary1, boundary2, processor); }
    },

    //example: object x = {}, reference "abc.defg.hij", result = x = { abc: { defg: { hij: value }}};
    setObjectAtReference: function (start, reference, value, delimiter) {
        var parts = reference.split(delimiter);
        var node = start;
        this.processIterator(0, parts.length - 2, i => {
            var part = parts[i];
            if (part != null && part.trim() != "" && node[part] == null) { node[part] = {}; }
            node = node[part];
        });
        node[parts[parts.length - 1]] = value;
    },

    orderPriority: function (priority, items, comparer) {
        var t = this;
        var result = [];
        if (items == null || items.length == 0) { return result; }
        if (items.length == 1 || priority == null || priority.length == 0) { return items; }
        if (comparer == null) { comparer = (a, b) => (a == b); }
        var ia = [];
        t.processArray(items, item => {
            var ip = -1;
            var fp = -1;
            t.processIterator(0, priority.length - 1, i => {
                if (comparer(priority[i], item[i]) === true) {
                    if (fp == -1) { fp = i; }
                    ip = i;
                }
                else if (fp != -1) { return false; }
            });
            ia.push({ i: item, f: fp, l: ip - fp });
        });
        ia.sort((a, b) => ((a.f - b.f) * priority.length + (b.l - a.l)));
        t.processArray(ia, ir => { result.push(ir.i); });
        return result;
    },

    transformEach: function (array, transform) {
        var result = [];
        if (array == null || array.length == 0) { return []; }
        if (transform == null) { return array; }
        this.processArray(array, a => result.push(transform(a)));
        return result;
    },

    wait: function (waitTime, maxWaits, isReadyFunc, resultProvider, climb) {
        var result = { waitTime: waitTime, maxWaits: maxWaits, isReadyFunc: isReadyFunc, waits: 0 };
        var time = 1;
        if (climb == null) { climb = true; }
        return new Promise((resolve, reject) => {
            var waiter = () => {
                if (isReadyFunc()) {
                    if (resultProvider != null) { result.result = resultProvider(); }
                    resolve(result);
                }
                else {
                    if (++(result.waits) > maxWaits) { reject(result); return; }
                    if (climb) {
                        time *= 2;
                        if (time >= waitTime) {
                            time = waitTime;
                            climb = false;
                        }
                    }
                    setTimeout(function () { waiter(); }, time);
                }                 
            }
            waiter();
        });
    },

    waitClimb: function (totalTime, maxTimeout, isReadyFunc) {
        var result = { totalTime: totalTime, maxTimeout: maxTimeout, isReadyFunc: isReadyFunc, waits: 0, elapsed: 0 };
        var time =1;
        var climb = true;
        return new Promise((resolve, reject) => {
            var waiter = () => {
                if (isReadyFunc(result)) { resolve(result); }
                else {
                    result.elapsed += time;
                    result.waits++;
                    var difference = totalTime - result.elapsed;
                    if (difference <= 0) { reject(result); return; }
                    if (climb) {
                        time *= 2;
                        if (time >= maxTimeout) {
                            time = maxTimeout;
                            climb = false;
                        }
                    }
                    if (difference < time) { time = difference; }
                    setTimeout(function () { waiter(); }, time);
                }
            }
            waiter();
        });
    },

    delay: function (time) {
        var done = false;
        return this.wait(time, 1, () => {
            var x = done ? true : false;
            done = true;
            return x;
        }, () => { }, false);
    },

    promise: function (result) {
        return new Promise((resolve, reject) => { resolve(result); });
    },

    promiseOrder: function (promises) {
        var t = this;
        if (promises == null || promises.length == 0) { return hularion.Control.Promise([]); }

        var pMap = new Map();
        var count = 0;
        hularion.Control.processArray(promises, promise => {
            var c = ++count;
            var item = { };
            promise.then(x => item.x = x);
            pMap.set(c, item);
        });
        return Promise.all(promises, () => {
            var result = [];
            hularion.Control.processIterator(1, count, i => {
                result.push(pMap.get(i).x);
            });
            return result;
        });
    }



};
