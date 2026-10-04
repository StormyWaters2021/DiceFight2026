// Shared data for the status-cue exploration. Icons are path data lifted from web/src/dicekingdom/icons.tsx.
export const ICONS = {"HermitCrabIcon":"<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"3\" d=\"M20,44 C14,34 18,18 34,16 C48,15 52,28 46,38 C40,47 26,48 20,44 Z\" /> <path d=\"M16,46 L8,42 M16,48 L8,52\" stroke=\"currentColor\" stroke-width=\"3\" stroke-linecap=\"round\" />","OpossumIcon":"<ellipse cx=\"32\" cy=\"34\" rx=\"18\" ry=\"9\" fill=\"currentColor\" /> <g stroke=\"currentColor\" stroke-width=\"3\" stroke-linecap=\"round\"> <path d=\"M18,27 L14,16\" /> <path d=\"M26,25 L24,14\" /> <path d=\"M38,25 L40,14\" /> <path d=\"M46,27 L50,16\" /> </g> <g stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\"> <path d=\"M27,32 L31,36 M31,32 L27,36\" /> <path d=\"M33,32 L37,36 M37,32 L33,36\" /> </g>","BarnOwlIcon":"<path d=\"M32,48 C14,34 14,18 32,20 C50,18 50,34 32,48 Z\" fill=\"currentColor\" />","ArmadilloIcon":"<path d=\"M10,48 C10,32 20,20 32,20 C44,20 54,32 54,48 Z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"4\" /> <line x1=\"10\" y1=\"48\" x2=\"54\" y2=\"48\" stroke=\"currentColor\" stroke-width=\"4\" />","WolfGlyphIcon":"<path fill=\"currentColor\" d=\"M14,14 L26,24 L32,18 L38,24 L50,14 L46,32 L52,40 L40,42 L32,56 L24,42 L12,40 L18,32 Z\" />","CuttlefishIcon":"<ellipse cx=\"30\" cy=\"26\" rx=\"14\" ry=\"10\" fill=\"currentColor\" /> <g stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" fill=\"none\"> <path d=\"M20,34 C18,42 16,48 14,52\" /> <path d=\"M26,36 C25,43 24,48 23,52\" /> <path d=\"M34,36 C35,43 36,48 37,52\" /> <path d=\"M40,34 C42,42 44,48 46,52\" /> </g> <circle cx=\"35\" cy=\"24\" r=\"2.2\" fill=\"#FFF8EC\" />","PangolinIcon":"<path fill=\"currentColor\" d=\"M42,14 C24,14 14,26 16,38 C17,46 24,52 34,50 C28,48 22,42 24,34 C26,26 34,22 42,24 C36,20 38,16 42,14 Z\" /> <g fill=\"none\" stroke=\"#FFF8EC\" stroke-width=\"2\"> <path d=\"M22,32 C26,30 30,30 33,32\" /> <path d=\"M20,38 C25,36 30,36 33,39\" /> </g>","FoxIcon":"<path fill=\"currentColor\" d=\"M14,20 C30,18 46,28 46,42 C36,44 24,40 20,30 C18,26 16,22 14,20 Z\" /> <path fill=\"#FFF8EC\" d=\"M46,42 C40,44 34,42 30,38 C36,36 42,38 46,42 Z\" />","HoneyBadgerIcon":"<g fill=\"currentColor\"> <polygon points=\"24,17 32,21.5 32,30.5 24,35 16,30.5 16,21.5\" /> <polygon points=\"40,17 48,21.5 48,30.5 40,35 32,30.5 32,21.5\" /> <polygon points=\"32,32 40,36.5 40,45.5 32,50 24,45.5 24,36.5\" /> </g>","TardigradeIcon":"<path fill=\"currentColor\" d=\"M14,30 C14,20 22,14 34,14 C46,14 54,21 54,30 C54,39 46,44 34,44 C20,44 14,40 14,30 Z\" /> <circle cx=\"13\" cy=\"28\" r=\"6\" fill=\"currentColor\" /> <g fill=\"currentColor\"> <rect x=\"18\" y=\"42\" width=\"5\" height=\"10\" rx=\"2.5\" /> <rect x=\"28\" y=\"43\" width=\"5\" height=\"10\" rx=\"2.5\" /> <rect x=\"38\" y=\"43\" width=\"5\" height=\"10\" rx=\"2.5\" /> <rect x=\"47\" y=\"41\" width=\"5\" height=\"10\" rx=\"2.5\" /> </g>","MuskOxIcon":"<circle cx=\"32\" cy=\"32\" r=\"7\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" /> <g stroke=\"currentColor\" stroke-width=\"3\" stroke-linecap=\"round\"> <path d=\"M32,18 L32,10\" /> <path d=\"M32,46 L32,54\" /> <path d=\"M18,32 L10,32\" /> <path d=\"M46,32 L54,32\" /> <path d=\"M22,22 L16,16\" /> <path d=\"M42,22 L48,16\" /> <path d=\"M22,42 L16,48\" /> <path d=\"M42,42 L48,48\" /> </g>","AnglerfishIcon":"<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.6\" stroke-linecap=\"round\" d=\"M18,46 C18,30 24,20 30,14\" /> <circle cx=\"30\" cy=\"12\" r=\"5\" fill=\"currentColor\" /> <g stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\"> <path d=\"M30,4 L30,1\" /> <path d=\"M22,8 L19,6\" /> <path d=\"M38,8 L41,6\" /> </g>","GrizzlyBearIcon":"<path d=\"M12,26 C16,18 28,15 38,20 C30,23 20,26 12,26 Z\" fill=\"currentColor\" /> <path d=\"M12,26 L5,20 L7,28 Z\" fill=\"currentColor\" /> <path fill=\"none\" stroke=\"currentColor\" stroke-width=\"4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"M12,42 L19,35 L26,42 L33,35 L40,42 L47,35 L54,42\" />","MongooseIcon":"<path fill=\"currentColor\" d=\"M8,36 C8,28 16,24 26,24 C36,24 46,28 52,32 C46,32 38,30 30,30 C34,34 34,40 28,44 C24,47 16,47 10,44 C15,43 19,40 19,36 C15,38 11,38 8,36 Z\" /> <polygon points=\"8,36 2,32 4,40\" fill=\"currentColor\" />"};
const ALIAS = {crab:'HermitCrabIcon',opossum:'OpossumIcon',owl:'BarnOwlIcon',armadillo:'ArmadilloIcon',wolf:'WolfGlyphIcon',cuttlefish:'CuttlefishIcon',pangolin:'PangolinIcon',fox:'FoxIcon',badger:'HoneyBadgerIcon',tardigrade:'TardigradeIcon',muskox:'MuskOxIcon',angler:'AnglerfishIcon',grizzly:'GrizzlyBearIcon',mongoose:'MongooseIcon'};
export function iconSrc(name, color){
  const body = ICONS[ALIAS[name]]; if(!body) return null;
  const svg = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" style="color:'+color+'">'+body.replace(/currentColor/g,color)+'</svg>';
  return 'data:image/svg+xml;utf8,'+encodeURIComponent(svg);
}
export const CAT = {
  combat:{name:'Combat',color:'oklch(0.85 0.15 88)',ink:'#14151c'},
  text:{name:'Abilities',color:'#fff8ec',ink:'#14151c'},
  pending:{name:'Pending KO',color:'oklch(0.66 0.2 25)',ink:'#ffffff'},
  away:{name:'Out of play',color:'#9198b0',ink:'#14151c'},
};
// fill: solid = has / must. outline+slash = can't / off.
export const META = {
  deadly:{name:'Deadly-engaged',cat:'pending',glyph:'KO',word:'KO AT END',fill:'solid',prio:1,mine:'Will be KO\'d at Clean Up, whatever its damage.',opp:'Will be KO\'d at Clean Up, whatever its damage.'},
  cantBlock:{name:"Can't block",cat:'combat',glyph:'B',word:"CAN'T BLOCK",fill:'outline',slash:true,prio:2,mine:"Can't be assigned as a blocker.",opp:"Can't block your attackers."},
  mustBlock:{name:'Must block',cat:'combat',glyph:'B!',word:'MUST BLOCK',fill:'solid',prio:2,mine:"Has to block if any attacker can be blocked. Blocks won't confirm without it.",opp:'Your opponent has to block with this if they can.'},
  unblockable:{name:'Unblockable',cat:'combat',glyph:'»',word:'UNBLOCKABLE',fill:'solid',prio:2,mine:'Can\'t be blocked, and neither can anything in its lane.',opp:'You can\'t block it, or anything in its lane.'},
  mustAttack:{name:'Must attack',cat:'combat',glyph:'A!',word:'MUST ATTACK',fill:'solid',prio:2,mine:'Has to attack if able.',opp:'Will be sent into a lane on their turn if able.'},
  cantAttack:{name:"Can't attack",cat:'combat',glyph:'A',word:"CAN'T ATTACK",fill:'outline',slash:true,prio:2,mine:"Can't be sent into a lane.",opp:"Can't attack you."},
  onlyBlocker:{name:'Only blocker',cat:'combat',glyph:'B1',word:'ONLY BLOCKER',fill:'solid',prio:2,mine:'The only die that can block this turn.',opp:'The only die that can block your attack.'},
  blanked:{name:'Text blanked',cat:'text',glyph:'T',word:'NO TEXT',fill:'outline',slash:true,prio:3,mine:'Abilities and keywords are off: a vanilla body.',opp:'Abilities and keywords are off: a vanilla body.'},
  granted:{name:'Granted keyword',cat:'text',glyph:'+',word:'+',fill:'solid',prio:4,mine:'Has a keyword its card doesn\'t print.',opp:'Has a keyword its card doesn\'t print.'},
  intimidated:{name:'Intimidated',cat:'away',glyph:'↩',word:'AWAY',fill:'outline',prio:2,mine:"Off the Field: can't block or be targeted, while-active effects are off. Returns on the same face.",opp:"Off the Field: can't block or be targeted, while-active effects are off. Returns on the same face."},
  locked:{name:'Locked out',cat:'text',glyph:'$',word:'LOCKED',fill:'outline',slash:true,prio:2,mine:"You can't buy or field this card.",opp:"Your opponent can't buy or field this card."},
};
export const KW = {OC:'Overcrush'};
const s=(k,src,dur,extra)=>Object.assign({k,src,dur},extra||{});
const T='This turn', CU='Until Clean Up';
export const SINGLES = [
  {key:'mustBlock',label:'Must block',die:{icon:'muskox',name:'Musk Ox',cost:2,atk:1,def:3,st:[s('mustBlock','Hermit Crab',T)]}},
  {key:'cantBlock',label:"Can't block",die:{icon:'fox',name:'Fox',cost:2,atk:2,def:2,st:[s('cantBlock','Barn Owl',T)]}},
  {key:'unblockable',label:'Unblockable',die:{icon:null,mono:'Ch',name:'Chameleon',cost:1,atk:2,def:1,st:[s('unblockable','Chameleon · Obscure',T)]}},
  {key:'mustAttack',label:'Must attack',die:{icon:'tardigrade',name:'Tardigrade',cost:0,atk:1,def:1,st:[s('mustAttack','(engine only)',T)]}},
  {key:'cantAttack',label:"Can't attack",die:{icon:'tardigrade',name:'Tardigrade',cost:0,atk:1,def:1,st:[s('cantAttack','(engine only)',T)]}},
  {key:'onlyBlocker',label:'Only blocker',die:{icon:'tardigrade',name:'Tardigrade',cost:0,atk:1,def:3,st:[s('onlyBlocker','(engine only)',T)]}},
  {key:'blanked',label:'Text blanked',die:{icon:'opossum',name:'Opossum',cost:2,atk:1,def:2,st:[s('blanked','(no DK card yet)','While source is active')]}},
  {key:'granted',label:'Granted keyword',die:{icon:'badger',name:'Honey Badger',cost:2,atk:5,def:2,batk:2,mods:'ATK 2 +3 Anger Issues',st:[s('granted','Anger Issues',T,{kw:'OC'})]}},
  {key:'deadly',label:'Deadly-engaged',die:{icon:'mongoose',name:'Mongoose',cost:2,atk:3,def:2,st:[s('deadly','Fought Opossum (Deadly)',CU)]}},
  {key:'damaged',label:'Damaged',die:{icon:'grizzly',name:'Grizzly Bear',cost:3,atk:4,def:4,dmg:2,st:[]}},
  {key:'buffed',label:'Buffed',die:{icon:'tardigrade',name:'Tardigrade',cost:0,atk:2,def:2,batk:1,bdef:1,mods:'ATK 1 +1 Queen Termite · DEF 1 +1 Queen Termite',st:[]}},
  {key:'debuffed',label:'Debuffed',die:{icon:'cuttlefish',name:'Cuttlefish',cost:3,atk:2,def:2,bdef:4,mods:'DEF 4 → 2 Archnemesis (D = A)',st:[]}},
  {key:'spun',label:'Spun down (flash)',die:{icon:'pangolin',name:'Pangolin',cost:2,atk:1,def:3,flash:'L3→L2',st:[]}},
  {key:'intimidated',label:'Intimidated · 40px row',size:40,die:{icon:'grizzly',name:'Grizzly Bear',cost:3,atk:4,def:4,st:[s('intimidated','Frilled Lizard · Intimidate',CU)]}},
  {key:'locked',label:'Locked card · Reserve',size:40,die:{icon:'muskox',name:'Musk Ox (card)',cost:2,atk:1,def:3,locked:true,st:[s('locked','Pangolin','While Pangolin is active')]}},
];
export const COMBOS = [
  {key:'c1',label:'Must block + damaged + buffed',dice:[{icon:'tardigrade',name:'Tardigrade',cost:0,atk:1,def:2,bdef:1,dmg:1,mods:'DEF 1 +1 Armadillo',st:[s('mustBlock','Hermit Crab',T)]}]},
  {key:'c2',label:'Unblockable + buffed + attacking',dice:[{icon:null,mono:'Ch',name:'Chameleon',cost:1,atk:5,def:1,batk:2,mods:'ATK 2 +3 Anger Issues',zone:'Attack · Lane 2',st:[s('unblockable','Chameleon · Obscure',T),s('granted','Anger Issues',T,{kw:'OC'})]}]},
  {key:'c3',label:'Deadly-engaged + damaged, in a lane',dice:[{icon:'mongoose',name:'Mongoose',cost:2,atk:3,def:2,dmg:1,zone:'Attack · Lane 1',st:[s('deadly','Fought Opossum (Deadly)',CU)]}]},
  {key:'c4',label:"Can't block + must block (can't wins)",dice:[{icon:'fox',name:'Fox',cost:2,atk:2,def:2,st:[s('mustBlock','Hermit Crab',T),s('cantBlock','Barn Owl',T)]}]},
  {key:'c5',label:'Blanked + must block',dice:[{icon:'opossum',name:'Opossum',cost:2,atk:1,def:2,st:[s('blanked','(no DK card yet)',T),s('mustBlock','Hermit Crab',T)]}]},
  {key:'c6',wide:true,label:'Two adjacent Unblockable Chameleons, next to a Must block',dice:[{icon:null,mono:'Ch',name:'Chameleon',cost:1,atk:2,def:1,st:[s('unblockable','Chameleon · Obscure',T)]},{icon:null,mono:'Ch',name:'Chameleon',cost:1,atk:2,def:1,st:[s('unblockable','Chameleon · Obscure',T)]},{icon:'fox',name:'Fox',cost:2,atk:2,def:2,st:[s('mustBlock','Hermit Crab',T)]}]},
  {key:'c7',label:'Overflow: four cues on one die',dice:[{icon:'badger',name:'Honey Badger',cost:2,atk:5,def:2,batk:2,dmg:1,mods:'ATK 2 +3 Anger Issues',st:[s('deadly','Fought Opossum (Deadly)',CU),s('mustBlock','Hermit Crab',T),s('blanked','(no DK card yet)',T),s('granted','Anger Issues',T,{kw:'OC'})]}]},
];
export function resolve(st){
  const has=k=>st.some(x=>x.k===k);
  return st.map(x=>Object.assign({},x,{over: x.k==='mustBlock'&&has('cantBlock')})).sort((a,b)=>META[a.k].prio-META[b.k].prio);
}
