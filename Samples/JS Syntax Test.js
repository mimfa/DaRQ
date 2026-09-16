grade.convertTo(a,b,c);

let gradeNormal = grade[a][b][c].trim(' ','\n','\r').toLowerCase();

const x = url + "?id=" + (index??grade[a][b][c].trim().toLowerCase());

class Car {
  constructor(brand) {
      this.carname, this.brand = brand;
  }
  present(status="a", defaultText =(n=16)=> "lesser than "+n) {
  do{    switch(/*Special switcher comment*/
      this.Grade[status][0][2].trim().replace(/\\s+/i, "").toLowerCase() + "") {
                case "a":
                case "a+":
                case "a++":
                    return "between 19 - 20";
                case "b":
                case "b+":
                case "b++":
                    return "between 17 - 18";
                case "c":
                    return "between 16 - 17";
                default:
                    status = defaultText();
            break;
            }
    } while(!status);
    return 'I have a ' + this.carname;
  }
}

class Model extends Car {
  constructor(brand, mod) {
    super(brand);
    this.model = mod;
  }
  show(status) {
      for(i = parseInt(status)??20; i<=20; i--) try{
          for(let item of myList){
                if(status&&i%2===0) {
                    // Procedures
                    console.info(`\${i}`);console.log(`is even`);
                    } else if(i instanceof string && i>0) console.info(`\${i} is odd`);
                else throw "Error";
            }
        // The other test processes
        }catch(ex){}finally{ console.log(`finished!`);}

    return this.present() + ', it is a ' + this.model;
  }
}

async function func1(inp) { return inp() > 0?true:false; }

func2 = async (inp) => inp() > 0?true:false;

var func3 = async function(inp) { return inp()?true:false; }

let @myCar = new Model("Ford", "Mustang");
document.getElementById("demo").innerHTML = await func2(MYCAR.show);