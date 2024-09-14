/*
MIT License

Copyright (c) 2023 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/



function ViewNavigationStack() {
    this.navPath = null;
    this.navArray = null;
    this.indices = new Map();
};
ViewNavigationStack.prototype = {

    setNavigationPath: function (navPath) {
        var t = this;
        t.navPath = navPath;
        //console.log("ViewNavigationStack.setNavigationPath - ", navPath);
        t.navPath.publisher.SegmentSelected.subscribe((o, index) => {
            //console.log("ViewNavigationStack.setNavigationPath SegmentSelected - ", index, t);
            if (t.navArray == null) { return; }
            t.navArray.publisher.ViewSelected.publish(index);
        });
        t.navPath.publisher.SegmentPreviewed.subscribe((o, index) => {
            //console.log("ViewNavigationStack.setNavigationPath SegmentPreviewed - ", index, t);
            if (t.navArray == null) { return; }
            t.navArray.publisher.ViewPreviewed.publish(index);
        });

    },

    setNavigationArray: function (navArray) {
        this.navArray = navArray;
    },

    addView: function (path, view) {
        var t = this;
        var result = {};
        if (t.navPath == null || t.navArray == null) { return; }
        var currentIndex = t.navPath.proxy.getSelectedIndex();
        t.navArray.proxy.remove(currentIndex + 1);
        var index = t.navArray.proxy.addView(view);
        result.pathSegment = t.navPath.proxy.addPath(path, index);
        t.navArray.proxy.selectView(index);
        return result;
    },

    setPathName: function (name) {
        var t = this;
        if (t.navPath == null) { return; }
        var segment = t.navPath.proxy.getSelectedSegment();
        t.navPath.proxy.setName(segment, name);
    }

};
