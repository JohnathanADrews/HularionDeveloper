/*
MIT License

Copyright (c) 2023 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/


function TreeTraverser() {
    this.LRPOrder = "LeftRightParent";
    this.RLPOrder = "RightLeftParent";
    this.PLROrder = "ParentLeftRight";
    this.PRLOrder = "ParentRightLeft";

    this.LeftWeaveOrder = "WeaveFromLeft";
    this.RightWeaveOrder = "WeaveFromRight";
};
TreeTraverser.prototype = {
    GenericTraversal: function (root, childProvider, parentFirst, processor, popProcessor) {
        var nodeStack = [];
        var indexStack = [];
        var lineageMap = new Map();
        var node = null;
        var index = 0;
        var children = null;

        nodeStack.push(root);
        indexStack.push(0);

        while (true) {
            node = nodeStack[nodeStack.length - 1];
            index = indexStack[indexStack.length - 1];
            if (!lineageMap.has(node)) { lineageMap.set(node, childProvider(node)); }
            children = lineageMap.get(node);
            if (parentFirst && index == 0 && !processor(node)) { return; }
            if (index >= children.length) {
                if (!parentFirst && !processor(node)) { return; }
                if (node === root) { break; }
                if (popProcessor != null) { popProcessor(node); }
                nodeStack.pop();
                indexStack.pop();
                indexStack[indexStack.length - 1]++;
                continue;
            }
            nodeStack.push(children[index]);
            indexStack.push(0);
        }
    },
    TraverseTree: function (order, root, childProvider, processor, popProcessor) {
        var t = this;
        var reverseProvider = function (node) {
            var children = childProvider(node);
            var result = [];
            for (var i = children.length - 1; i >= 0; i++) { result.push(children[i]); }
        }
        if (order == t.LRPOrder) {
            t.GenericTraversal(root, childProvider, false, processor, popProcessor);
        }
        else if (order == t.RLPOrder) {
            t.GenericTraversal(root, reverseProvider, false, processor, popProcessor);
        }
        else if (order == t.PLROrder) {
            t.GenericTraversal(root, childProvider, true, processor, popProcessor);
        }
        else if (order == t.PRLOrder) {
            t.GenericTraversal(root, reverseProvider, true, processor, popProcessor);
        }
    },
    CreateEvaluationPlan: function (order, root, childProvider, includeLeaves) {
        if (root == null) { return null; }
        var t = this;
        var result = [];
        t.TraverseTree(order, root, childProvider, function (node) {
            if (includeLeaves || childProvider(node).length > 0) {
                result.push(node);
                //console.log("TreeTraverser.CreateEvaluationPlan - ", node);
            } return true;
        });
        return result;
    },
    WeaveExecute: function (order, root, childProvider, entryAction, upAction, exitAction) {
        var t = this;
        var traverseOrder = t.PLROrder;
        if (order == t.RightWeaveOrder) {
            traverseOrder = TreeTraverser.PRLOrder;
        }
        var childrenMap = new Map();
        var stateMap = new Map();

        var rootChildren = childProvider(root);
        childrenMap.set(root, rootChildren);
        stateMap.set(root, { subject: root, parent: null, isRoot: true, isLeaf: (rootChildren.length == 0) });

        t.TraverseTree(traverseOrder, root,
            function (node) {
                var children = childrenMap.get(node);
                var grandchildren = [];
                children.forEach(function (child) {
                    grandchildren = childProvider(child);
                    childrenMap.set(child, grandchildren);
                    stateMap.set(child, { subject: child, parent: node, isRoot: false, isLeaf: (grandchildren.length == 0) });
                });
                return children;
            },
            function (node) {
                entryAction(stateMap.get(node));
                return true;
            },
            function (node) {
                var state = stateMap.get(node);
                var siblings = childrenMap.get(state.parent);
                exitAction(state);
                if (node !== siblings[siblings.length - 1]) {
                    upAction(stateMap.get(state.parent));
                }
            });
        exitAction(stateMap.get(root));
    },
    RenderExpression: function (root, childProvider, leftClosure, nodeHandler, rightClosure, options) {
        //root: The root node.
        //childProvider: provides the child nodes of a given node. e.g.  node => node.nodes;
        //leftClosure: The function that will render the left closure of the expression.
        //nodeHandler: The function that handles the display of the node. node => { ... }
        //rightClosure: The function that will render the right closure of the expression.
        //displayReverse: Iff true, the expression will be processed in reverse order.
        var order = TreeTraverser.TreeWeaveOrder.FromLeft;
        if (options == null) {
            options = { displayReverse: false, useLeafClosure: true, useOuterClosure: true }
        };

        var leftClosureProxy = function (node) {
            if (node.isLeaf && options.useLeafClosure === false) { return; }
            if (node.isRoot && options.useOuterClosure === false) { return; }
            leftClosure();
        };
        var rightClosureProxy = function (node) {
            if (node.isLeaf && options.useLeafClosure === false) { return; }
            if (node.isRoot && options.useOuterClosure === false) { return; }
            rightClosure();
        };

        if (options.displayReverse === true) { order = TreeTraverser.TreeWeaveOrder.FromRight; }

        TreeTraverser.WeaveExecute(order, root, childProvider,
            node => {
                leftClosureProxy(node);
                if (node.isLeaf) { nodeHandler(node.subject); }
                else if (childProvider(node.subject).length == 1) { nodeHandler(node.subject); }
            },
            node => nodeHandler(node.subject),
            node => rightClosureProxy(node));
    },
}



