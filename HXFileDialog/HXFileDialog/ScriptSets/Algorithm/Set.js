/*
MIT License

Copyright (c) 2023 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/


function Set() {
};
Set.prototype = {

    Union: function (set1, set2, comparer) {
        var t = this;
        var map = new Map();
        hularion.Control.ProcessArray(set1, item => { map.set(item, item); });
        hularion.Control.ProcessArray(set2, item => { map.set(item, item); });
        var result = [];
        map.forEach((value, key, map) => { result.push(key); });
        return result;
    },

    Intersect: function (set1, set2, comparer) {
        var t = this;
        var map = new Map();
        var result = [];
         hularion.Control.ProcessArray(set1, item => { map.set(item, item); });
         hularion.Control.ProcessArray(set2, item => {
            if (map.has(item)) { result.push(item); }
        });
        return result;
    },

    Subtract: function (set, subtraction, comparer) {
        var t = this;
        var map = new Map();
        var result = [];
         hularion.Control.ProcessArray(subtraction, item => { map.set(item, item); });
         hularion.Control.ProcessArray(set, item => {
            if (!map.has(item)) { result.push(item); }
        });
        return result;
    },

    //Complements set with the properties in complement
    Complement: function (set, complement) {
        var t = this;
        if (set == null) { set = {}; }
        var pairs =  hularion.Control.GetObjectMemberNameValuePairs(complement);
         hularion.Control.ProcessArray(pairs, pair => {
            if (set[pair.name] == null) { set[pair.name] = pair.value; }
        });
        return set;
    },

    Clone: function (set) {
        var t = this;
        var o = {};
        return t.Complement(o, set);
    },

    InitializeObject: function (object, parameters, defaults) {
        var t = this;
        var x = t.Complement(parameters, defaults);
        t.Complement(object, x);
    },

    InitializeOobject: function (object, parameters, defaults) {
        var t = this;
        t.InitializeObject(object, parameters, defaults);
    },

    IsIn: function (item, values, comparer) {
        var t = this;
        return false ===  hularion.Control.ProcessArray(values, value => {
            if (comparer(item, value)) { return false; }
        });
    }
};
