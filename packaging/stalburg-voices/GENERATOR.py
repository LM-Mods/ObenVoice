#!/usr/bin/env python3
"""ObenVoice private NPC 20-voice laboratory generator.

Generate 20 experimental Kokoro style tensors from locally available upstream
Kokoro-82M voice tensor files. This does NOT train voices or provide independently
synthesised speech/audio, accents, speed control or authentic character casting.

Only intended for private listening trials. Not executed by ObenVoice or by game.
Usage:
  python ObenVoice-NPC-Voice-Lab-20-Generator.py --sources-dir ./upstream-voices --output-dir ./generated-voices
Requires Python 3.10+ and torch, in a development environment only.
"""
from __future__ import annotations
import argparse
from pathlib import Path
import hashlib
import json
import math
import torch

REVISION = 'b0bbc5ad2fbb5c0164e809871c41b00c0c3b0b19'
SOURCE_ORIGIN = 'https://huggingface.co/hexgrad/Kokoro-82M'
# Role / NPC inspiration describes creative INTENT, not sound validated through listening.
# Smaller coefficients -> safer styles. Every major candidate uses a distinct native anchor,
# except M10, an intentionally experimental second style derived from a British anchor.
# fields: serial, voice ID, short name, gender, parent source, NPC inspiration,
# role design, (timbre PCA coefficients), (prosody PCA coefficients), deviation, seed
DESIGNS = [
 ('M01','am_oben20_brann','Brann','male','am_fenrir','Malone Petronis','weathered biker, restrained rather than cheerful',(-0.22,0.11),(-0.26,-0.10),0.23,6101),
 ('M02','am_oben20_eero','Eero','male','bm_lewis','Pate Rantanen','tired older tenement resident',(-0.33,-0.10),(-0.40,-0.12),0.22,6102),
 ('M03','am_oben20_kemal','Kemal','male','em_alex','Akim Azizi','busy street food vendor; non-Nordic candidate',(0.23,-0.13),(0.18,-0.06),0.23,6103),
 ('M04','am_oben20_vasko','Vasko','male','im_nicola','Pentti Penttilä','unhurried basement bar proprietor',(-0.12,0.24),(-0.28,0.04),0.20,6104),
 ('M05','am_oben20_kiro','Kiro','male','pm_alex','Ivan Kapustanov','practical bazaar trader; stronger exploration',(-0.75,0.58),(-0.72,-0.47),0.75,6105),
 ('M06','am_oben20_hakon','Hakon','male','bm_fable','Prison Guard (generic label)','controlled and stern authority figure',(-0.35,0.17),(-0.27,0.14),0.18,6106),
 ('M07','am_oben20_niko','Niko','male','am_puck','Aleksi Kivi','restless young resident',(0.25,-0.20),(0.19,0.26),0.21,6107),
 ('M08','am_oben20_veld','Veld','male','am_echo','Wizard Welhö','dry eccentric storyteller',(0.17,0.24),(-0.22,0.15),0.25,6108),
 ('M09','am_oben20_kram','Kram','male','hm_omega','Golem / strange creature archetype','unusual, deliberate, nonhuman-inspired',(0.28,-0.22),(-0.41,-0.20),0.27,6109),
 ('M10','am_oben20_otto','Otto','male','bm_lewis','Gustav','sociable bakery regular; alternate-style stress test',(0.82,-0.74),(0.84,0.62),0.83,6110),
 ('F01','af_oben20_aila','Aila','female','bf_isabella','Agnes Takala','calm pragmatic resident',(-0.21,-0.15),(-0.36,0.10),0.20,6201),
 ('F02','af_oben20_nessa','Nessa','female','af_nicole','Pia Horn','low-key, grounded local',(-0.35,0.12),(-0.43,-0.08),0.20,6202),
 ('F03','af_oben20_zora','Zora','female','pf_dora','Zoya Gubenko','direct bazaar visitor; non-Nordic candidate',(0.19,-0.24),(0.15,-0.09),0.22,6203),
 ('F04','af_oben20_vika','Vika','female','ff_siwis','Irmeli Nakkila','brisk no-nonsense shopkeeper; French-anchor trial',(0.20,0.17),(0.32,-0.11),0.23,6204),
 ('F05','af_oben20_hilde','Hilde','female','af_kore','Anneli Tuoppi','measured mature voice',(-0.24,0.21),(-0.35,0.15),0.18,6205),
 ('F06','af_oben20_oona','Oona','female','af_aoede','Aune Korsborg','alert and professional employee',(0.18,-0.16),(0.18,0.22),0.21,6206),
 ('F07','af_oben20_tasha','Tasha','female','if_sara','Tatyana Gopnikova','distinct visiting voice with non-English background',(-0.16,0.27),(-0.15,-0.21),0.23,6207),
 ('F08','af_oben20_minna','Minna','female','af_nova','Toinu Karhu','younger everyday resident',(0.28,-0.08),(0.25,0.18),0.20,6208),
 ('F09','af_oben20_shade','Shade','female','jf_alpha','Shadow Woman','uncanny, deliberately unusual candidate',(-0.33,-0.23),(-0.40,-0.21),0.24,6209),
 ('F10','af_oben20_rima','Rima','female','hf_alpha','Tonie Savisaar','warm but worldly restaurant trader',(0.25,0.19),(0.21,-0.18),0.21,6210),
]


def digest(path:Path)->str:
    h=hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda:stream.read(1024*1024),b''):h.update(chunk)
    return h.hexdigest()


def load_voice(path):
    x=torch.load(path,map_location='cpu',weights_only=True)
    if not isinstance(x,torch.Tensor) or x.shape!=(510,1,256) or x.dtype!=torch.float32 or not bool(torch.isfinite(x).all()):
        raise ValueError(f'Invalid Kokoro float32 510x1x256 tensor: {path.name}')
    return x


def pca_basis(voices:list[torch.Tensor],lo:int,hi:int):
    # Speaker centroid is sampled from mid-length rows. These tensor halves are
    # manipulated as tentative style channels; labeling them timbre/prosody is
    # heuristic, not a proven acoustic decomposition.
    centers=torch.stack([v[64:480,0,lo:hi].mean(dim=0) for v in voices])
    shifted=centers-centers.mean(dim=0)
    _u,_s,vt=torch.linalg.svd(shifted,full_matrices=False)
    basis=vt[:max(2,min(6,len(voices)-1))].clone()
    for row in basis:
        peak=int(row.abs().argmax())
        if row[peak]<0: row *= -1
    return basis,float(torch.median(torch.pdist(centers)))


def orthogonal_vector(basis:torch.Tensor,seed:int):
    g=torch.Generator(device='cpu').manual_seed(seed)
    v=torch.randn(basis.shape[1],generator=g,dtype=torch.float32)
    for row in basis:v-=torch.dot(v,row)*row
    v/=v.norm().clamp(min=1e-8)
    return v


def procedural(anchor,pcabasis,scale,coeff,amount,seed,lo,hi):
    # Use learned principal directions plus original seeded orthogonal direction,
    # NOT simple weighted blending of input voices. Magnitudes bounded to stay
    # near original speaker style. No claim of naturalness without audio test.
    tangent = coeff[0]*pcabasis[0]+coeff[1]*pcabasis[1]
    tangent += 0.16*orthogonal_vector(pcabasis,seed)
    tangent *= scale*amount
    max_len=scale*0.32 if amount<=0.30 else scale*0.56
    tangent *= min(1.0,max_len/max(1e-8,float(tangent.norm())))
    rows=torch.arange(510,dtype=torch.float32)
    length_env=0.72+0.28*torch.sigmoid((rows-40)/32)
    return anchor[:,0,lo:hi] + length_env[:,None]*tangent[None,:]


def build(source_dir:Path,out_dir:Path):
    out_dir.mkdir(parents=True,exist_ok=True)
    used_names=sorted(set(d[4] for d in DESIGNS))
    loaded={name:load_voice(source_dir/(name+'.pt')) for name in used_names}
    genders={k:[] for k in ('male','female')}
    for d in DESIGNS:genders[d[3]].append(d[4])
    for group in genders:genders[group]=list(dict.fromkeys(genders[group]))
    pcab={}
    for g,names in genders.items():
        tensors=[loaded[n] for n in names]
        pcab[g]={span:pca_basis(tensors,*span) for span in ((0,128),(128,256))}
    manifest={'manifestVersion':1,'id':'oben-npc-voice-lab-20',
      'name':'ObenVoice NPC Voice Lab — 20 (PRIVATE TEST)','engine':'kokoro','voices':[]}
    provenance={'status':'Private untested experimental Kokoro style data, not independently trained speakers',
      'derivation':'9 male and 10 female native source anchors; bounded, seeded PCA style steering and a small orthogonal perturbation; M10 shares a source anchor with M02',
      'anchorSource':'hexgrad/Kokoro-82M','license':'Apache-2.0',
      'sourceFiles':[], 'outputs':[]}
    for name in used_names:
        path=source_dir/(name+'.pt')
        provenance['sourceFiles'].append({'name':name+'.pt','sha256':digest(path),
          'origin':SOURCE_ORIGIN+'/blob/'+(REVISION if name not in ('hm_omega','hf_alpha','jf_alpha','ff_siwis') else 'main')+'/voices/'+name+'.pt',
          'sourceRevision': REVISION if name not in ('hm_omega','hf_alpha','jf_alpha','ff_siwis') else 'unverified revision; file identified by SHA256'})
    for serial,voiceid,label,gender,source,npc,intent,tc,pc,amount,seed in DESIGNS:
        out=loaded[source].clone()
        for lo,hi,coeff in ((0,128,tc),(128,256,pc)):
            b,scale=pcab[gender][(lo,hi)]
            out[:,0,lo:hi]=procedural(out,b,scale,coeff,amount,seed+(lo==128)*9001,lo,hi)
        if out.dtype!=torch.float32 or out.shape!=(510,1,256) or not bool(torch.isfinite(out).all()):raise ValueError(voiceid)
        if torch.equal(out,loaded[source]):raise ValueError('Unchanged voice '+voiceid)
        fname=voiceid+'.pt'
        path=out_dir/fname
        torch.save(out,path)
        if not torch.equal(out,load_voice(path)):raise ValueError('Roundtrip failed '+voiceid)
        manifest['voices'].append({'id':voiceid,'displayName':f'{serial} {label} — {intent.split(";")[0][:30]}',
          'gender':gender,'file':'voices/'+fname})
        provenance['outputs'].append({'number':serial,'id':voiceid,'sourceAnchor':source,
          'inspiredBy':npc,'intentNotConfirmed':intent,
          'styleSteering':{'first128':tc,'last128':pc,'amount':amount,'seed':seed},
          'sha256':digest(path),'audioVerified':False})
    return manifest,provenance


if __name__=='__main__':
    ap=argparse.ArgumentParser()
    ap.add_argument('--sources-dir',required=True,type=Path)
    ap.add_argument('--output-dir',required=True,type=Path)
    args=ap.parse_args()
    m,p=build(args.sources_dir,args.output_dir)
    print(json.dumps({'total':len(m['voices']),'male':sum(v['gender']=='male' for v in m['voices']),
      'female':sum(v['gender']=='female' for v in m['voices']),
      'outputs':[x['id'] for x in p['outputs']]},indent=2))
