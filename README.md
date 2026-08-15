# DaRQ (Declarative Automation Rule Query Language)

DaRQ is a deterministic, reusable, human-readable, extensible command language for acquiring, transforming, analyzing, and automating data workflows. A structured intermediate representation (IR) between natural language and executable JavaScript scraping logic. In other words, DaRQ is a procedural, English-like domain-specific language that compiles deterministically into JavaScript browser automation code.
* Scripts written by this technology called "Map" will parse into complex and optimized pure JavaScript code to be interpreted in the browser.
* You can write pure JavaScript between your Map freely.

Start DaRQ directly using the tutorial sample [here](SAMPLE.md) quickly...

Also you can find more examples and their parsed JS versions [here](Samples/).


## Main Principles
DaRQ has **five** main principles:
1. Every Statement will be the same as English grammar.
    ```js
    FOR EACH url OF list, LOAD url, THEN APPEND ALL "h2" TO destination;
    ```
2. Every Command will be compiled deterministically into JavaScript.
    * There is no hidden behavior.
    * Never runtime guessing.
3. The current execution Variables are always available.
4. Everything is extensible except the grammar.
5. The generated JavaScript is the executable truth.


## Main Syntax
In the DaRQ:
* Everything is is built upon four+1 fundamental concepts: DaRQ **Structures**, **Commands**, **Selectors**, Workspace **Variables**, and **Reserves**.
    * **Structures** define the grammar.
    * **Commands** perform actions.
    * **Selectors** locate and identify resources.
    * **Variables** of the Workspace represent the current execution context.
    * **Reserves** represent all reserved-words or noise-words defined for DaRQ.
* Using predefined structures and commands, you will be able to write your procedure more clearly, more human-readable,  and more flexibly.
* Case statements of DaRQ are very flexible (case-relieve), instead of being somewhere:
    * Case-insensitive:
        * All JS statements, such as IF, FUNCTION, LET, etc.
        * All DaRQ statements, such as FOR EACH, TRY TO, OTHERWISE, etc.
        * All DaRQ workspace variables, such as BROWSER, WINDOW, RESPONSE, etc.
        * All DaRQ global variables, such as POST_METHOD, GET_METHOD, THAT, BACK, etc.
        * All DaRQ reserved-words, such as FROM, AND, BE etc.
        * All DaRQ built-in commands, ALL, USE, RESERVE, etc.
        * Multiple of the most important window variables, such as DOCUMENT, CONSOLE, SCREEN, etc.
        * Multiple of the most important window functions, such as ALERT, CONFIRM, PROMPT, etc.
        * All third-party commands, FILE, FOLDER, TEXT, etc.
        * All user-defined commands.
        * All user-defined reserved-words.
    * Case-sensitive:
        * All user-defined functions/variables/constants.
        * All objects' functions/variables/constants (almost everything is written after a dot('.')).

    ### Structures:
    The DaRQ engine makes multiple predefined structures, which the user cannot override.
    * Built into the compiler.
    * They define the grammar and cannot be overridden (IF, FOR, COMMAND, USE, NOISE, ALL, ONE, ON, etc.)
    * Multiple of the most important DaRQ Structures are:

        #### Conditions
        * To work in a special conditions
            ```js
            IF condition, procedure; ELSE procedure;
            ```
            It will compile to:
            ```js
            if(js-condition)
                js-procedure;
            else 
                js-procedure;
            ```

        #### Iterations
        * Working with a collection based iterations
            ```js
            FOR item OF/IN collection/object, procedure;
            ```
            ```js
            EACH item OF/IN collection/object, procedure;
            ```
            ```js
            FOR EACH item OF/IN collection/object, procedure;
            ```
            All of them will compile to:
            ```js
            for(js-item of js-collection)
                js-procedure;
            ```
            or
            ```js
            for(js-item in js-object)
                js-procedure;
            ```
        * Using a conditional iterations
            ```js
            WHILE condition, procedure;
            ```
            It will compile to:
            ```js
            while(js-condition)
                js-procedure;
            ```
        * Using a post conditional iterations
            ```js
            DO { js-procedures } WHILE condition;
            ```
            It will compile to:
            ```js
            do {
                js-procedures
            } while(js-condition);
            ```

        #### Blockings
        * To make a pure JS procedures-block
            ```js
            {
                js-procedures
            }
            ```
            It will directly compile to a JS procedures block
        * To make a DaRQ procedures-block
            ```js
            DO procedures END;
            ```
            It will compile to:
            ```js
            {
                js-procedures
            }
            ```
        * To make a DaRQ callable procedures-block
            ```js
            DOING procedures END;
            ```
            It will compile to:
            ```js
            () => {
                js-procedures
            };
            ```
        * To make a tryable block
            ```js
            TRY procedures; CATCH problem, procedure; FINALLY procedure;
            ```
            It will compile to:
            ```js
            try {
                js-procedures
            } catch (problem) {
                js-procedures
            } finally {
                js-procedures
            }
            ```

        #### CallLabels
        * To make a simple callable with these features:
            * Will have a special labelName
            * Will call immediately after definition once
            * Will call just using the name without any parentesis
            * To define:
                ```js
                #labelName procedure;
                ```
                It will compile to:
                ```js
                var labelName = () =>
                    js-procedure;
                labelName();
                ```
            * To use:
                ```js
                labelName
                ```
                It will compile to:
                ```js
                labelName()
                ```

        #### Threadings
        * To have a multi-thread program, you can make and manage promises
            ```js
            PROMISE procedure THEN procedure; OTHERWISE procedure; ANYWAY procedure;
            ```
            It will compile to:
            ```js
            new Promise(()=>js-procedure)
                .then((data)=>js-procedure)
                .catch((data)=>js-procedure)
                .finally((data)=>js-procedure)
            ```

        #### Collections
        * To work in collections
            ```js
            array WHERE condition
            ```
            It will compile to:
            ```js
            js-array.filter(data=>js-condition)
            ```
            ```js
            array ORDER BY key1, key2
            ```
            It will compile to:
            ```js
            js-array
                .sort((a, b)=>(a,b)=>a.js-key1>b.js-key1?1:a.js-key1==b.js-key1?0:-1)
                .sort((a, b)=>(a,b)=>a.js-key2>b.js-key2?1:a.js-key2==b.js-key2?0:-1)
            ```
            ```js
            array LIMIT number, count
            ```
            It will compile to:
            ```js
            js-array.slice(js-number, js-count)
            ```
            ...


    ### Commands:
    All commands represent executable operations.
    * Can be overridden in code.
    * A very small set that the compiler itself needs for parsing or semantic analysis defined built in.
    * They will include the following cases:
        * All simple DaRQ built-in commands.
        * All third-party commands (you add them to your Map using the USE statement).
        * All user-defined commands (You defined using the COMMAND statement).
        * You can call commands in four ways:
            * Grammatical:			
                ```js
                CMD1                            		// To call without any inputted arguments
                ```
            * Functional:			
                ```js
                CMD1(val1_1, CMD2(val2_1, val2_2))		// Exact calling a function
                ```
            * Procedural:			
                ```js
                CMD1 val1_1, CMD2 val2_1, val2_2	    // Separated by comma.
                ```
            * Optional:			
                ```js
                CMD1 {									// Sending an object of named values
                    inp1_1: val1_1,
                    inp1_2: CMD2 { inp2_1: val2_1, inp2_2: val2_2 } 
                }
                ```
        * You can combine the methods above, depending on your code situations.
        * All of the ways will convert to a simple function call.
    * Every command behaves like a JavaScript function while following several conventions:
        * All commands must defiine globally.
        * All inputs of each command should be optional. So ensure every parameter has a default value.
        * If the command has no returned output, it should return itself (specially when there is not inputted arguments). 
        * The `commandName` will be added to the JS parser of the DaRQ engine.
        * The user-defined command will be accessible exactly like other global commands, too.
    * You can make your special commands using the following syntax:
        ```js
            COMMAND commandName(input1=value1, ...) {	// All inputs of each command must have a default value
                // procedures
                /*
                    // Should return one of the following
                    return output;
                    return commandName; // If you don't have any return value, you should return the command directly (usually when you do not have input arguments).
                    // It will be useful when  you want to send a callable version of the command to another one as an input
                */
            }
        ```


    ### Selectors:
    Selectors describe what should be selected, but not only DOM elements, potentially anything.
    * Built into the compiler.
    * They define how resources are identified (CSS, XPath, RegEx, Id, Class, Index, Location, ...)
    * You have multiple special predefined DaRQ commands in core, to extract elements from the resource too:
        ```js
        ALL selector						// To select all elements using the specified selector from the DOCUMENT
        ```
        ```js
        ONE selector						// To select one element using the specified selector from the DOCUMENT
        ```
        ```js
        ON selector					    	// To select all/one elements using the specified selector from the DOCUMENT
        ```
        ```js
        ALL/ONE/ON selector FROM parent 	// To select all/one from the parent element/object/array
        ```
        ```js
        resource[selector]				    // To select one item using the specified selector on the resource, the resource can be an element/object/array
        ```
    * Selectors effectively makes DaRQ a universal extraction language, not just a scripting language, Because you can use one of the DaRQ Selectors with the syntax below, everywhere you need:
        * CSS:
            ```js
            table>tbody>tr			    // Write CSS selector directly, But if it has spaces or {} should wrapped by single/double quotes
            ```
        * XPath:
            ```js
            \table\tbody\tr\	        // Using backslash wrapped path to use a XPath route
            ```
        * RegEx:
            ```js
            /(?<=<tr>)(?=<\/tr>)/gi		// You can use a Regular Expression Pattern to filter elements based on their outerHTML
            ```
        * Tag:
            ```js
            tagName					    // Select element/s using type directly the elementName
            ```
        * Id:
            ```js
            #tagId				    // Select an element using type element Id after a #
            ```
        * Class:
            ```js
            .tagClass					// Select element/s using type element class after a dot('.')
            ```
        * Location:
            ```js
            500, 700, 999				// The location of element on the screen(x,y,z) or a dimensional array or object
            ```
        * Index:
            ```js
            12							// The index of the child
            ```


    ### Workspaces:
    There are multiple defined variables accessible globally, which users can interact with.
    * A very small set of them defined bult-in (APPLICATION, BROWSER, WINDOW, TAB, DOCUMENT, RESPONSE, THAT, ...)
    * Some of the global variables that will update based on the current status are named Workspace, including:
        ```js
        BROWSER 			    // The current browser used in this thread
        ```
        ```js
        APPLICATION 		    // The current application (MiMFa Scraper) used in this thread
        ```
        ```js
        WINDOW/TAB 		        // The current window/tab used in this thread
        ```
        ```js
        DOCUMENT 		        // The current loaded document in the window used in this thread
        ```
        ```js
        RESPONSE			    // The latest response received in this thread
        ```
        ```js
        LOADED				    // The current document used in this thread was loaded successfully or not
        ```
        ```js
        FETCHED			        // The current response used in this thread was received successfully or not
        ```
        ```js
        THAT				    // The output of the previous execution will keep on that
        ```
    * Multiple workspace variables will update in the current thread, based on the two commands FETCH/LOAD status.
        * FETCH: To send a request to a server and receive its response, and update the workspace based on that, using the syntax below:
            ```js
            FETCH url, data, method		// To fetch data from a specified URL, then update the Workspace
            ```
        * LOAD: To open a website in the current window, and update the workspace based on that, using the syntax below:
            ```js
            LOAD url		// To load a website from a specified URL in the current window/tab, and then update the Workspace
            ```
            ```js
            LOAD NEXT		// To load a website from a specified URL in the current window/tab, and then update the Workspace
            ```
            ```js
            LOAD BACK		// To load a website from a specified URL in the current window/tab, and then update the Workspace
            ```
            ```js
            LOAD			// To update the Workspace based on the current window/tab statements
            ```
    * If you want to work in your browser, window, or so on, without changing the current workspace, use the following commands:
        * GET: To send a GET request to a server and receive its response, using the syntax below:
            ```js
            GET url, data		// To fetch data from a specified URL
            ```
        * POST: To send a POST request to a server and receive its response, using the syntax below:
            ```js
            POST url, data		// To fetch data from a specified URL
            ```
        * GO: To send a request to a server and receive its response, without changing the current workspace, using the syntax below:
            ```js
            GO url			// To load a website from a specified URL in a new window/tab
            ```
            ```js
            GO NEXT		    // To open the next window/tab, if it exists
            ```
            ```js
            GO BACK		    // To open the previous window/tab, if it exists 
            ```


    ### Reserves:
    DaRQ allows optional readability words that improve the natural flow of the language. They will affect only the parser and contain no JavaScript code. These can be one of two groups below:
    * Reserved words: All words will be replaced directly in the script while being preserved in the source code.
        * The user can call any one of the codes below freely.
        * The reserved name will be completely case-insensitive.
        * It can improve the readability of the code with a predefined replacement for them.
        * You can add a reserved-word to the parser using the following syntax:
            ```js
            RESERVE reservedWordName AS "replacement";       // This line affects only the parser, and will replace the used reservedWordName by replacement everywhere it used.
            ```
        * For example, you can write any one of the bellow codes, without any effects in meaning:
            ```js
            RESERVE into AS ",";
            // Other procedures
            SAVE "hello world!" INTO "index.html";
            // With the same meaning
            SAVE "hello world!" , "index.html";
            ```
            Or:
            ```js
            RESERVE restart AS "LOAD \"http://mimfa.net\";";
            // Other procedures
            RESTART
            // With the same meaning
            LOAD "http://mimfa.net";
            ```
        * Parser will replace those specified words before parsing by default.
    * Noise-words: All words are ignored semantically while being preserved in the source code.
        * The user can call any one of the codes below freely.
        * The reserved name will be completely case-insensitive.
        * It can improve the readability of the code without carrying any semantic meaning on them.
        * You can add a noise word to the parser using the following syntax:
            ```js
            RESERVE noiseWordName;       // This line affects only the parser, not JavaScript.
            ```
        * For example, you can write any one of the bellow codes, without any effects in meaning:
            ```js
            RESERVE the;
            // Other procedures
            CLICK ON THE #nextbutton
            // With the same meaning
            CLICK ON #nextbutton
            ```
            Or:
            ```js
            RESERVE a;
            // Other procedures
            LOAD A "www.example.com"
            // With the same meaning
            LOAD "www.example.com"
            ```
        * Parser will skip those specified words before parsing by default.