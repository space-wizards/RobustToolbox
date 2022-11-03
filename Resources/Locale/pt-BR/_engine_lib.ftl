# Used internally by the THE() function.
zzzz-the = { PROPER($ent) ->
    *[false] a { $ent }
     [true] { $ent }
    }

# Used internally by the SUBJECT() function.
zzzz-subject-pronoun = { GENDER($ent) ->
    [male] ele
    [female] ela
    [epicene] eles
   *[neuter] ele
   }

# Used internally by the OBJECT() function.
zzzz-object-pronoun = { GENDER($ent) ->
    [male] ele
    [female] ela
    [epicene] eles
   *[neuter] isso
   }

# Used internally by the POSS-PRONOUN() function.
zzzz-possessive-pronoun = { GENDER($ent) ->
    [male] dele
    [female] dela
    [epicene] deles
   *[neuter] dele
   }

# Used internally by the POSS-ADJ() function.
zzzz-possessive-adjective = { GENDER($ent) ->
    [male] dele
    [female] dela
    [epicene] deles
   *[neuter] dele
   }

# Used internally by the REFLEXIVE() function.
zzzz-reflexive-pronoun = { GENDER($ent) ->
    [male] ele mesmo
    [female] ela mesmo
    [epicene] eles mesmos
   *[neuter] ele mesmo
   }

# Used internally by the CONJUGATE-BE() function.
zzzz-conjugate-be = { GENDER($ent) ->
    [epicene] é
   *[other] é
   }

# Used internally by the CONJUGATE-HAVE() function.
zzzz-conjugate-have = { GENDER($ent) ->
    [epicene] tenho
   *[other] tem
   }

# Used internally by the CONJUGATE-BASIC() function.
zzzz-conjugate-basic = { GENDER($ent) ->
    [epicene] { $first }
   *[other] { $second }
   }
